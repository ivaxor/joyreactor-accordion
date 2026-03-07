using JoyReactor.Accordion.Logic.Database.Sql;
using JoyReactor.Accordion.Logic.Database.Vector;
using JoyReactor.Accordion.Logic.Database.Vector.Extensions;
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
        var statitics = new StatisticsResponse();

        var vectors = qdrantClient.CountAsync(qdrantSettings.Value.CollectionName, cancellationToken);
        statitics.Vectors = Convert.ToInt32(await vectors);

        statitics.ParsedTags = await sqlDatabaseContext.ParsedTags.CountAsync(cancellationToken);
        statitics.EmptyTags = await sqlDatabaseContext.EmptyTags.CountAsync(cancellationToken);

        statitics.ParsedPosts = await sqlDatabaseContext.ParsedPosts.CountAsync(cancellationToken);

        var parsedPostAttributePictures = await sqlDatabaseContext.ParsedPostAttributePictures
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                NoContent = g.Count(p => p.NoContent == true),
                Unsupported = g.Count(p => p.UnsupportedContent == true),
                WithVector = g.Count(p => p.IsVectorCreated == true),
                WithoutVector = g.Count(p => p.IsVectorCreated == false && p.NoContent == false && p.UnsupportedContent == false)
            })
            .FirstAsync(cancellationToken);
        statitics.ParsedPostAttributePictures = parsedPostAttributePictures.Total;
        statitics.ParsedPostAttributePicturesNoContent = parsedPostAttributePictures.NoContent;
        statitics.ParsedPostAttributePicturesUnsupported = parsedPostAttributePictures.Unsupported;
        statitics.ParsedPostAttributePicturesWithVector = parsedPostAttributePictures.WithVector;
        statitics.ParsedPostAttributePicturesWithoutVector = parsedPostAttributePictures.WithoutVector;

        statitics.ParsedPostAttributeEmbeds = await sqlDatabaseContext.ParsedPostAttributeEmbeds.CountAsync(cancellationToken);

        statitics.ParsedBandCamps = await sqlDatabaseContext.ParsedBandCamps.CountAsync(cancellationToken);
        statitics.ParsedCoubs = await sqlDatabaseContext.ParsedCoubs.CountAsync(cancellationToken);
        statitics.ParsedSoundClouds = await sqlDatabaseContext.ParsedSoundClouds.CountAsync(cancellationToken);
        statitics.ParsedVimeos = await sqlDatabaseContext.ParsedVimeos.CountAsync(cancellationToken);
        statitics.ParsedYouTubes = await sqlDatabaseContext.ParsedYouTubes.CountAsync(cancellationToken);

        return statitics;
    }
}