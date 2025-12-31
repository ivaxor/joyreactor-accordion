using JoyReactor.Accordion.Logic.Database.Vector.Entities;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace JoyReactor.Accordion.Logic.Database.Vector;

public class VectorDatabaseContext(
    IQdrantClient qdrantClient,
    IOptions<QdrantSettings> settings)
    : IVectorDatabaseContext
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
            scoreThreshold: settings.Value.SearchScoreThreshold,
            cancellationToken: cancellationToken);

        return results
            .Select(result => new ImagePayload(result))
            .ToArray();
    }
}

public interface IVectorDatabaseContext
{
    Task InsertAsync(float[] vector, CancellationToken cancellationToken = default);
    Task<ImagePayload[]> SearchAsync(float[] vector, CancellationToken cancellationToken = default);
}