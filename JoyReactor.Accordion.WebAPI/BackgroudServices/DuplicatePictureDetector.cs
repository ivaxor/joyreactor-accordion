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
                    .OrderBy(ppap => ppap.AttributeId)
                    .FirstAsync(cancellationToken);
                duplicatePictureIdIndex.Value = initialPicture.AttributeId.ToString();
            }

            var attributeIdFrom = int.Parse(duplicatePictureIdIndex.Value);

            pictures = await sqlDatabaseContext.ParsedPostAttributePictures
                .Where(ppap => ppap.IsVectorCreated == true)
                .Where(ppap => ppap.AttributeId >= attributeIdFrom)
                .OrderBy(ppap => ppap.AttributeId)
                .Take(100)
                .ToArrayAsync(cancellationToken);

            logger.LogInformation("Starting searching duplicates for {PicturesCount} post attribute picture(s).", pictures.Length);

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

                var latestVector = response.Result
                    .OrderByDescending(p => p.Payload["contentVersion"].IntegerValue)
                    .First();

                var similarVectors = await qdrantClient.RecommendAsync(
                    qdrantSettings.Value.CollectionName,
                    positive: [new Guid(latestVector.Id.Uuid)],
                    filter: new Filter()
                    {
                        Must =
                        {
                            new Condition()
                            {
                                Field = new FieldCondition()
                                {
                                    Key = "postAttributeId",
                                    Range = new Qdrant.Client.Grpc.Range()
                                    {
                                        Gt = picture.AttributeId
                                    }
                                }
                            }
                        }
                    },
                    scoreThreshold: 0.90f,
                    limit: 10,
                    vectorsSelector: new WithVectorsSelector() { Enable = false },
                    payloadSelector: new WithPayloadSelector() { Enable = true },
                    cancellationToken: cancellationToken);

                if (similarVectors.Count == 0)
                    continue;

                var originalPoint = new PictureRetrivedPoint(latestVector);

                var duplicateVectors = similarVectors
                    .Select(v => new PictureScoredPoint(v))
                    .GroupBy(p => p.PostId, (key, collection) => collection.OrderByDescending(e => e.ContentVersion).First())
                    .ToArray();

                var votes = duplicateVectors
                    .Select(v => new DuplicatePictureVote(originalPoint, v))
                    .ToArray();

                logger.LogInformation("Found {DuplicatesCount} duplicates for {PictureAttributeId} post attribute picture.", duplicateVectors.Length, originalPoint.PostAttributeId);

                await sqlDatabaseContext.DuplicatePictureVotes.AddRangeAsync(votes, cancellationToken);
            }

            duplicatePictureIdIndex.Value = (pictures.Last().AttributeId + 1).ToString();
            await sqlDatabaseContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("{PicturesCount} post attribute picture(s) were searched for duplicates.", pictures.Length);

        } while (pictures.Length > 0);
    }
}