using JoyReactor.Accordion.Logic.Database.Sql;
using JoyReactor.Accordion.Logic.Database.Vector;
using JoyReactor.Accordion.WebAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using System.Collections.Frozen;

namespace JoyReactor.Accordion.WebAPI.BackgroudServices;

public class VectorPostAttributeCleaner(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<QdrantSettings> qdrantSettings,
    IOptions<BackgroundServiceSettings> settings,
    ILogger<VectorNormalizator> logger)
    : RobustBackgroundService(settings, logger)
{
    protected override bool IsIndefinite => false;

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        await using var serviceScope = serviceScopeFactory.CreateAsyncScope();
        using var sqlDatabaseContext = serviceScope.ServiceProvider.GetRequiredService<SqlDatabaseContext>();
        var qdrantClient = serviceScope.ServiceProvider.GetRequiredService<IQdrantClient>();

        var postAttributeContentVersions =
            (await sqlDatabaseContext.ParsedPostAttributePictures
            .AsNoTracking()
            .Include(picture => picture.Post)
            .OrderBy(picture => picture.AttributeId)
            .ToDictionaryAsync(picture => picture.AttributeId, picture => picture.Post.ContentVersion, cancellationToken))
            .ToFrozenDictionary();

        var knownPostAttributeIds = new HashSet<int>(postAttributeContentVersions.Count);
        var pointIdsToDelete = new List<PointId>();

        var scrollOffset = (PointId)null;
        var scrollResponse = (ScrollResponse)null;
        do
        {
            scrollResponse = await qdrantClient.ScrollAsync(
                collectionName: qdrantSettings.Value.CollectionName,
                limit: 50000,
                offset: scrollOffset,
                vectorsSelector: false,
                payloadSelector: true,
                cancellationToken: cancellationToken);
            scrollOffset = scrollResponse.NextPageOffset;

            logger.LogInformation("Checking {VectorCount} vector(s) for cleanup.", scrollResponse.Result.Count);
            foreach (var scrollPoint in scrollResponse.Result)
            {
                if (!scrollPoint.Payload.TryGetValue("postAttributeId", out var postAttributeIdValue))
                    continue;

                if (!scrollPoint.Payload.TryGetValue("contentVersion", out var contentVersionValue))
                    continue;

                var postAttributeId = Convert.ToInt32(postAttributeIdValue.IntegerValue);
                var contentVersion = Convert.ToInt32(contentVersionValue.IntegerValue);

                if (!postAttributeContentVersions.TryGetValue(postAttributeId, out var latestContentVersion))
                    continue;

                if (contentVersion != latestContentVersion)
                {
                    pointIdsToDelete.Add(scrollPoint.Id);
                    continue;
                }

                if (!knownPostAttributeIds.Add(postAttributeId))
                {
                    pointIdsToDelete.Add(scrollPoint.Id);
                    continue;
                }
            }
        } while (scrollResponse.Result.Count != 0 && scrollResponse.NextPageOffset != null);

        await qdrantClient.DeleteAsync(
                collectionName: qdrantSettings.Value.CollectionName,
                ids: pointIdsToDelete,
                cancellationToken: cancellationToken);
        logger.LogInformation("Deleted {VectorCount} vector(s) after cleanup.", pointIdsToDelete.Count);
    }
}