using JoyReactor.Accordion.Logic.Database.Sql;
using JoyReactor.Accordion.Logic.Database.Sql.Entities;
using JoyReactor.Accordion.Logic.Database.Vector;
using JoyReactor.Accordion.Logic.Database.Vector.Entities;
using JoyReactor.Accordion.WebAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using System.Collections.Frozen;
using Range = Qdrant.Client.Grpc.Range;

namespace JoyReactor.Accordion.WebAPI.BackgroudServices;

public class VectorNormalizator(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<QdrantSettings> qdrantSettings,
    IOptions<BackgroundServiceSettings> settings,
    ILogger<VectorNormalizator> logger)
    : RobustBackgroundService(settings, logger)
{
    protected override TimeSpan SubsequentRunDelay => TimeSpan.FromDays(1);
    protected override bool IsIndefinite => false;
    protected const int BatchSize = 100_000;

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        await using var serviceScope = serviceScopeFactory.CreateAsyncScope();
        using var sqlDatabaseContext = serviceScope.ServiceProvider.GetRequiredService<SqlDatabaseContext>();
        var qdrantClient = serviceScope.ServiceProvider.GetRequiredService<IQdrantClient>();

        var fromAttributeId = 0;
        var toAttributeId = BatchSize;

        Dictionary<int, ParsedPostAttributePicture> postAttributeByAttributeId = null;
        do
        {
            var postAttributesChangeCount = 0;
            postAttributeByAttributeId = await sqlDatabaseContext.ParsedPostAttributePictures
                .AsNoTracking()
                .Include(picture => picture.Post)
                .Where(picture => picture.AttributeId >= fromAttributeId && picture.AttributeId < toAttributeId)
                .OrderBy(picture => picture.AttributeId)
                .ToDictionaryAsync(picture => picture.AttributeId, cancellationToken);

            var scrollResponse = await qdrantClient.ScrollAsync(
                collectionName: qdrantSettings.Value.CollectionName,
                filter: new Filter()
                {
                    Must =
                    {
                    new Condition()
                    {
                        Field = new FieldCondition()
                        {
                            Key = "postAttributeId",
                            Range = new Range()
                            {
                                Gte = fromAttributeId,
                                Lt = toAttributeId,
                            }
                        }
                    }
                    }
                },
                limit: BatchSize * 2,
                vectorsSelector: false,
                payloadSelector: true,
                orderBy: new OrderBy()
                {
                    Key = "postAttributeId",
                    Direction = Direction.Asc,
                },
                cancellationToken: cancellationToken);
            var retrivedPoints = scrollResponse.Result.Select(p => new PictureRetrivedPoint(p)).ToArray();

            logger.LogInformation("Checking {VectorCount} vector(s) agains {SqlCount} SQL record(s) for cleanup.", retrivedPoints.Length, postAttributeByAttributeId.Count);

            var retrivedPointsByPostAttributeId = retrivedPoints
                .GroupBy(p => p.PostAttributeId.Value)
                .ToDictionary(g => g.Key, g => g.OrderBy(p => p.ContentVersion).ToArray())
                .ToFrozenDictionary();

            var oldContentVersionPointIds = new List<Guid>();
            foreach (var (postAttributeId, points) in retrivedPointsByPostAttributeId)
            {
                if (!postAttributeByAttributeId.ContainsKey(postAttributeId))
                {
                    var pointIds = points.Select(p => p.PointId).ToArray();
                    oldContentVersionPointIds.AddRange(pointIds);
                }
                else if (points.Length != 1)
                {
                    var pointIds = points.SkipLast(1).Select(p => p.PointId).ToArray();
                    oldContentVersionPointIds.AddRange(pointIds);
                }
            }

            foreach (var (postAttributeId, postAttribute) in postAttributeByAttributeId)
            {
                if (!postAttribute.IsVectorCreated)
                    continue;

                if (retrivedPointsByPostAttributeId.TryGetValue(postAttributeId, out var existingRetrivedPoints))
                {
                    var latestContentVersionRetrivedPoint = existingRetrivedPoints.Last();
                    if (latestContentVersionRetrivedPoint.ContentVersion == postAttribute.Post.ContentVersion)
                        continue;

                    oldContentVersionPointIds.Add(latestContentVersionRetrivedPoint.PointId);
                    postAttribute.IsVectorCreated = false;
                    postAttribute.UpdatedAt = DateTime.UtcNow;
                    postAttributesChangeCount++;
                }
                else
                {
                    postAttribute.IsVectorCreated = false;
                    postAttribute.UpdatedAt = DateTime.UtcNow;
                    postAttributesChangeCount++;
                }
            }

            if (oldContentVersionPointIds.Count > 0)
            {
                await qdrantClient.DeleteAsync(
                    collectionName: qdrantSettings.Value.CollectionName,
                    ids: oldContentVersionPointIds,
                    cancellationToken: cancellationToken);
                logger.LogInformation("Deleted {VectorCount} vector(s).", oldContentVersionPointIds.Count);
            }

            if (postAttributesChangeCount > 0)
            {
                await sqlDatabaseContext.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Updated {SqlCount} SQL record(s).", postAttributesChangeCount);
            }

            fromAttributeId += BatchSize;
            toAttributeId += BatchSize;
        } while (postAttributeByAttributeId.Count != 0);
    }
}