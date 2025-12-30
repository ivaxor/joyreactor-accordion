using JoyReactor.Accordion.Database.Models;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace JoyReactor.Accordion.Database;

public class DatabaseWrapper(
    IQdrantClient qdrantClient,
    IOptions<QdrantSettings> settings)
    : IDatabaseWrapper
{
    public async Task InsertAsync(float[] vector, CancellationToken cancellationToken = default)
    {
        var point = new PointStruct
        {
            Id = Guid.NewGuid(),
            Vectors = vector,
            Payload = {
                ["postIds"] = new string [] { },
                ["commentIds"] = new string [] { },
                ["immageIds"] = new string [] { },
            },
        };

        await qdrantClient.UpsertAsync(
            settings.Value.CollectionName,
            [point],
            cancellationToken: cancellationToken);
    }

    public async Task<ImagePayload[]> SearchAsync(float[] vector, CancellationToken cancellationToken = default)
    {
        var results = await qdrantClient.SearchAsync(
            settings.Value.CollectionName,
            vector,
            limit: settings.Value.SearchLimit,
            scoreThreshold: settings.Value.ScoreThreshold,
            cancellationToken: cancellationToken);

        return results
            .Select(result => DeserializePayload(result))
            .ToArray();
    }

    internal static ImagePayload DeserializePayload(ScoredPoint result)
    {
        var payload = new ImagePayload
        {
            PostIds = result.Payload.TryGetValue("postIds", out var postIdsValue) && postIdsValue.KindCase == Value.KindOneofCase.ListValue
                ? postIdsValue.ListValue.Values.Select(v => v.StringValue).ToHashSet(StringComparer.Ordinal)
                : [],
            CommentIds = result.Payload.TryGetValue("commentIds", out var commentIdsValue) && commentIdsValue.KindCase == Value.KindOneofCase.ListValue
                ? commentIdsValue.ListValue.Values.Select(v => v.StringValue).ToHashSet(StringComparer.Ordinal)
                : [],
            ImageIds = result.Payload.TryGetValue("imageIds", out var imageIdsValue) && imageIdsValue.KindCase == Value.KindOneofCase.ListValue
                ? imageIdsValue.ListValue.Values.Select(v => v.StringValue).ToHashSet(StringComparer.Ordinal)
                : [],
        };

        return payload;
    }
}

public interface IDatabaseWrapper
{
    Task InsertAsync(float[] vector, CancellationToken cancellationToken = default);
    Task<ImagePayload[]> SearchAsync(float[] vector, CancellationToken cancellationToken = default);
}