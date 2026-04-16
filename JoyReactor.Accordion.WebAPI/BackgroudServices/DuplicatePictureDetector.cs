using JoyReactor.Accordion.Logic.Database.Sql;
using JoyReactor.Accordion.Logic.Database.Sql.Entities;
using JoyReactor.Accordion.Logic.Database.Vector;
using JoyReactor.Accordion.Logic.Database.Vector.Entities;
using JoyReactor.Accordion.Logic.Extensions;
using JoyReactor.Accordion.WebAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace JoyReactor.Accordion.WebAPI.BackgroudServices;

public class DuplicatePictureDetector(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<BackgroundServiceSettings> settings,
    IOptions<QdrantSettings> qdrantSettings,
    ILogger<DuplicatePictureDetector> logger)
    : RobustBackgroundService(settings, logger)
{
    protected static readonly int BatchSize = 10000;
    protected override bool IsIndefinite => true;

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        await using var serviceScope = serviceScopeFactory.CreateAsyncScope();
        await using var sqlDatabaseContext = serviceScope.ServiceProvider.GetRequiredService<SqlDatabaseContext>();
        var qdrantClient = serviceScope.ServiceProvider.GetRequiredService<IQdrantClient>();

        var pictures = (ParsedPostAttributePicture[])null;

        do
        {
            pictures = await sqlDatabaseContext.ParsedPostAttributePictures
                .Where(ppap => ppap.IsVectorCreated == true && ppap.IsVectorCheckedForDuplicates == false)
                .OrderBy(ppap => ppap.AttributeId)
                .Take(BatchSize)
                .ToArrayAsync(cancellationToken);

            if (pictures.Length == 0)
                return;

            var foundTotal = 0;
            var upsertedTotal = 0;

            logger.LogInformation("Starting searching duplicates for {PicturesCount} post attribute picture(s).", pictures.Length);

            foreach (var picture in pictures)
            {
                picture.IsVectorCheckedForDuplicates = true;

                // https://joyreactor.cc/tag/%D0%B1%D0%B0%D1%8F%D0%BD
                // Только посты после 15 ноября 2017 года могут быть баянами
                if (picture.PostId.ToInt() < 3302432)
                    continue;

                var response = await qdrantClient.ScrollAsync(
                    qdrantSettings.Value.CollectionName,
                    filter: new Filter()
                    {
                        Must =
                        {
                            new Condition()
                            {
                                Field = new FieldCondition()
                                {
                                    Key = "postAttributeId",
                                    Match = new Match() { Integer = picture.AttributeId }
                                },
                            },
                        },
                    },
                    vectorsSelector: new WithVectorsSelector() { Enable = false },
                    payloadSelector: new WithPayloadSelector() { Enable = true },
                    cancellationToken: cancellationToken);

                if (response.Result.Count == 0)
                    logger.LogError("Failed to find vector for {PictureAttributeId} post attribute picture.", picture.AttributeId);

                var initialPoint = response.Result
                    .Select(v => new PictureRetrivedPoint(v))
                    .OrderBy(p => p.ContentVersion)
                    .Last();

                var similarVectors = await qdrantClient.RecommendAsync(
                    qdrantSettings.Value.CollectionName,
                    positive: [initialPoint.PointId],
                    filter: new Filter()
                    {
                        MustNot =
                        {
                            new Condition()
                            {
                                Field = new FieldCondition()
                                {
                                    Key = "postId",
                                    Match = new Match() { Integer = initialPoint.PostId.Value }
                                }
                            },
                            new Condition()
                            {
                                Field = new FieldCondition()
                                {
                                    Key = "postAttributeId",
                                    Match = new Match() { Integer = initialPoint.PostAttributeId.Value }
                                }
                            },
                        }
                    },
                    scoreThreshold: 0.99f,
                    limit: 25,
                    vectorsSelector: new WithVectorsSelector() { Enable = false },
                    payloadSelector: new WithPayloadSelector() { Enable = true },
                    cancellationToken: cancellationToken);

                if (similarVectors.Count == 0)
                    continue;

                var similarPoints = similarVectors
                    .Select(v => new PictureScoredPoint(v))
                    .Where(p => p.PostId != initialPoint.PostId)
                    .Where(p => p.PostAttributeId != initialPoint.PostAttributeId)
                    .Where(p => Math.Abs(p.PostId.Value - initialPoint.PostId.Value) >= 10)
                    .GroupBy(p => p.PostId, (key, collection) => collection.OrderByDescending(e => e.ContentVersion).First())
                    .ToArray();

                var similarPointPostAttributeIds = similarPoints
                    .Select(p => p.PostAttributeId)
                    .ToArray();

                var existingSimilarPointPostAttributeIds = await sqlDatabaseContext.ParsedPostAttributePictures
                    .AsNoTracking()
                    .Where(p => similarPointPostAttributeIds.Contains(p.AttributeId))
                    .Select(p => p.AttributeId)
                    .ToArrayAsync(cancellationToken);

                if (similarPointPostAttributeIds.Length != existingSimilarPointPostAttributeIds.Length)
                {
                    var missingSimilarPoints = similarPointPostAttributeIds.Length - existingSimilarPointPostAttributeIds.Length;
                    logger.LogWarning("{PointCount} similar point(s) no longer exists in SQL database.", missingSimilarPoints);
                }

                var votes = similarPoints
                    .Where(similarPoint => existingSimilarPointPostAttributeIds.Contains(similarPoint.PostAttributeId.Value))
                    .Select(similarPoint => initialPoint.PostId < similarPoint.PostId
                        ? new DuplicatePictureVote(initialPoint, similarPoint)
                        : new DuplicatePictureVote(similarPoint, initialPoint))
                    .ToArray();

                logger.LogDebug("Found {DuplicatesCount} duplicate(s) for {PictureAttributeId} post attribute picture.", similarPoints.Length, initialPoint.PostAttributeId);

                var votesUpserted = await sqlDatabaseContext.DuplicatePictureVotes
                    .UpsertRange(votes)
                    .On(v => new { v.OriginalPictureId, v.DuplicatePictureId })
                    .NoUpdate()
                    .RunAsync(cancellationToken);

                foundTotal += votes.Length;
                upsertedTotal += votesUpserted;
            }

            await sqlDatabaseContext.SaveChangesAsync(cancellationToken);
            sqlDatabaseContext.ChangeTracker.Clear();

            logger.LogInformation("Found {DuplicatesCount} and upserted {DuplicatesCount} post attribute picture duplicate(s) as votes.", foundTotal, upsertedTotal);
        } while (pictures.Length > 0);
    }
}