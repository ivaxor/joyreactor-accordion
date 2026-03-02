using JoyReactor.Accordion.Logic.Database.Sql;
using JoyReactor.Accordion.Logic.Database.Sql.Entities;
using JoyReactor.Accordion.Logic.Database.Vector;
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

        do
        {
            var duplicatePictureIdIndex = await sqlDatabaseContext.Configs.FirstAsync(c => c.Value == ConfigConstants.DuplicatePictureIdIndex, cancellationToken);
            if (string.IsNullOrWhiteSpace(duplicatePictureIdIndex.Value))
            {
                var initialPicture = await sqlDatabaseContext.ParsedPostAttributePictures
                    .OrderBy(ppap => ppap.AttributeId)
                    .FirstAsync(cancellationToken);
                duplicatePictureIdIndex.Value = initialPicture.AttributeId.ToString();
            }

            var attributeIdFrom = int.Parse(duplicatePictureIdIndex.Value);

            var pictures = await sqlDatabaseContext.ParsedPostAttributePictures
                .Where(ppap => ppap.IsVectorCreated == true)
                .Where(ppap => ppap.AttributeId > attributeIdFrom)
                .Take(100)
                .ToArrayAsync(cancellationToken);

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
                                    Key = "postId",
                                    Match = new Match() { Integer = picture.AttributeId }
                                },
                            },
                        },
                    },
                    vectorsSelector: new WithVectorsSelector() { Enable = false },
                    payloadSelector: new WithPayloadSelector() { Enable = true },
                    cancellationToken: cancellationToken);

                var vector = response.Result
                    .OrderByDescending(r => r.Payload["contentVersion"].IntegerValue)
                    .First();

                var similarVectors = await qdrantClient.RecommendAsync(
                    qdrantSettings.Value.CollectionName,
                    positive: [new Guid(vector.Id.Uuid)],
                    scoreThreshold: 0.95f,
                    limit: 10,
                    vectorsSelector: new WithVectorsSelector() { Enable = false },
                    payloadSelector: new WithPayloadSelector() { Enable = true },
                    cancellationToken: cancellationToken);

                if (similarVectors.Count == 0)
                    continue;


            }

        } while (true);
    }
}