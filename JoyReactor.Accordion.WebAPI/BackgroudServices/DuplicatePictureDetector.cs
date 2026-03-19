using JoyReactor.Accordion.Logic.Database.Sql;
using JoyReactor.Accordion.Logic.Database.Sql.Entities;
using JoyReactor.Accordion.Logic.Database.Vector;
using JoyReactor.Accordion.Logic.Database.Vector.Entities;
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
            var duplicatePictureIdIndex = await sqlDatabaseContext.Configs.FirstAsync(c => c.Name == ConfigConstants.DuplicatePictureIdIndex, cancellationToken);
            if (string.IsNullOrWhiteSpace(duplicatePictureIdIndex.Value))
            {
                var initialPicture = await sqlDatabaseContext.ParsedPostAttributePictures
                    .AsNoTracking()
                    .OrderBy(ppap => ppap.AttributeId)
                    .FirstAsync(cancellationToken);
                duplicatePictureIdIndex.Value = initialPicture.AttributeId.ToString();
            }

            var attributeIdFrom = int.Parse(duplicatePictureIdIndex.Value);

            pictures = await sqlDatabaseContext.ParsedPostAttributePictures
                .AsNoTracking()
                .Where(ppap => ppap.IsVectorCreated == true)
                .Where(ppap => ppap.AttributeId >= attributeIdFrom)
                .OrderBy(ppap => ppap.AttributeId)
                .Take(BatchSize)
                .ToArrayAsync(cancellationToken);
            var votesCount = 0;

            logger.LogInformation("Starting searching duplicates for {PicturesCount} post attribute picture(s) after {PictureAttributeId} picture attribute id.", pictures.Length, attributeIdFrom);

            foreach (var picture in pictures)
            {
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
                            }
                        }
                    },
                    scoreThreshold: 0.99f,
                    limit: 100,
                    vectorsSelector: new WithVectorsSelector() { Enable = false },
                    payloadSelector: new WithPayloadSelector() { Enable = true },
                    cancellationToken: cancellationToken);

                if (similarVectors.Count == 0)
                    continue;

                var similarPoints = similarVectors
                    .Select(v => new PictureScoredPoint(v))
                    .Where(p => p.PostId != initialPoint.PostId)
                    .Where(p => p.PostAttributeId != initialPoint.PostAttributeId)
                    .GroupBy(p => p.PostId, (key, collection) => collection.OrderByDescending(e => e.ContentVersion).First())
                    .ToArray();

                var votes = similarPoints
                    .Select(similarPoint => initialPoint.PostAttributeId < similarPoint.PostAttributeId
                        ? new DuplicatePictureVote(initialPoint, similarPoint)
                        : new DuplicatePictureVote(similarPoint, initialPoint))
                    .ToArray();
                votesCount += votes.Length;

                logger.LogDebug("Found {DuplicatesCount} duplicate(s) for {PictureAttributeId} post attribute picture.", similarPoints.Length, initialPoint.PostAttributeId);

                await sqlDatabaseContext.DuplicatePictureVotes.AddRangeAsync(votes, cancellationToken);
            }

            duplicatePictureIdIndex.Value = (pictures.Last().AttributeId + 1).ToString();
            await sqlDatabaseContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Found {DuplicatesCount} post attribute picture duplicate(s).", votesCount);
        } while (pictures.Length > 0);
    }
}