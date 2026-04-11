using JoyReactor.Accordion.Logic.Database.Sql;
using JoyReactor.Accordion.Logic.Database.Vector;
using JoyReactor.Accordion.Logic.Database.Vector.Extensions;
using JoyReactor.Accordion.Logic.Extensions;
using JoyReactor.Accordion.WebAPI.Models.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Qdrant.Client;

namespace JoyReactor.Accordion.WebAPI.Controllers;

[Route("statistics")]
[ApiController]
[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
public class StatisticsController(
    IMemoryCache memoryCache,
    SqlDatabaseContext sqlDatabaseContext,
    IQdrantClient qdrantClient,
    IOptions<QdrantSettings> qdrantSettings)
    : ControllerBase
{
    [HttpGet]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    [AllowAnonymous]
    [ProducesResponseType<StatisticsResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken = default)
    {
        const string cacheKey = $"{nameof(StatisticsController)}.{nameof(GetAsync)}";
        var response = await memoryCache.GetOrCreateAsync(cacheKey, cacheEntry =>
        {
            cacheEntry.SlidingExpiration = TimeSpan.FromMinutes(1);
            return GetStatisticsAsync(cancellationToken);
        });

        return Ok(response);
    }

    protected async Task<StatisticsResponse> GetStatisticsAsync(CancellationToken cancellationToken)
    {
        const string statisticsSql = @"
SET LOCAL jit = off;
SELECT
    0 AS ""Vectors"",

    (SELECT COUNT(*) FROM ""ParsedTags"") AS ""ParsedTags"",
    (SELECT COUNT(*) FROM ""EmptyTags"") AS ""EmptyTags"",

    (SELECT COUNT(*) FROM ""ParsedPosts"") AS ""ParsedPosts"",

    (SELECT COUNT(*) FROM ""ParsedPostAttributePictures"") AS ""ParsedPostAttributePictures"",
    (SELECT COUNT(*) FROM ""ParsedPostAttributePictures"" WHERE ""NoContent"") AS ""ParsedPostAttributePicturesNoContent"",
    (SELECT COUNT(*) FROM ""ParsedPostAttributePictures"" WHERE ""NoContentDueToDns"") AS ""ParsedPostAttributePicturesNoContentDueToDns"",
    (SELECT COUNT(*) FROM ""ParsedPostAttributePictures"" WHERE ""UnsupportedContent"") AS ""ParsedPostAttributePicturesUnsupported"",
    (SELECT COUNT(*) FROM ""ParsedPostAttributePictures"" WHERE ""IsVectorCreated"") AS ""ParsedPostAttributePicturesWithVector"",
    (SELECT COUNT(*) FROM ""ParsedPostAttributePictures"" WHERE NOT ""IsVectorCreated"" AND NOT ""NoContent"" AND NOT ""NoContentDueToDns"" AND NOT ""UnsupportedContent"") AS ""ParsedPostAttributePicturesWithoutVector"",
    (SELECT COUNT(*) FROM ""ParsedPostAttributePictures"" WHERE ""IsVectorCheckedForDuplicates"") AS ""ParsedPostAttributePicturesCheckedForDuplicates"",

    (SELECT COUNT(*) FROM ""ParsedPostAttributeEmbeds"") AS ""ParsedPostAttributeEmbeds"",

    (SELECT COUNT(*) FROM ""ParsedBandCamps"") AS ""ParsedBandCamps"",
    (SELECT COUNT(*) FROM ""ParsedCoubs"") AS ""ParsedCoubs"",
    (SELECT COUNT(*) FROM ""ParsedSoundClouds"") AS ""ParsedSoundClouds"",
    (SELECT COUNT(*) FROM ""ParsedVimeos"") AS ""ParsedVimeos"",
    (SELECT COUNT(*) FROM ""ParsedYouTubes"") AS ""ParsedYouTubes""
";

        var (vectors, statistics) = await TaskTyped.WhenAll(
            qdrantClient.CountAsync(qdrantSettings.Value.CollectionName, cancellationToken),
            sqlDatabaseContext.Database.SqlQueryRaw<StatisticsResponse>(statisticsSql).AsAsyncEnumerable().FirstAsync(cancellationToken).AsTask());

        statistics.Vectors = Convert.ToInt32(vectors);

        return statistics;
    }
}