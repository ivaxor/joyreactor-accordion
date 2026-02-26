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
        var statitics = new StatisticsResponse();

        var (vectors,
            parsedTags, emptyTags, parsedPosts,
            parsedPostAttributePictures, parsedPostAttributeEmbeds,
            parsedBandCamps, parsedCoubs, parsedSoundClouds, parsedVimeos, parsedYouTubes) = await TaskTyped.WhenAll(
            qdrantClient.CountAsync(qdrantSettings.Value.CollectionName, cancellationToken),

            sqlDatabaseContext.ParsedTags.CountAsync(cancellationToken),
            sqlDatabaseContext.EmptyTags.CountAsync(cancellationToken),
            sqlDatabaseContext.ParsedPosts.CountAsync(cancellationToken),

            sqlDatabaseContext.ParsedPostAttributePictures
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    NoContent = g.Count(p => p.NoContent == true),
                    Unsupported = g.Count(p => p.UnsupportedContent == true),
                    WithVector = g.Count(p => p.IsVectorCreated == true),
                    WithoutVector = g.Count(p => p.IsVectorCreated == false)
                })
                .FirstAsync(cancellationToken),
            sqlDatabaseContext.ParsedPostAttributeEmbeds.CountAsync(cancellationToken),

        sqlDatabaseContext.ParsedBandCamps.CountAsync(cancellationToken),
            sqlDatabaseContext.ParsedCoubs.CountAsync(cancellationToken),
            sqlDatabaseContext.ParsedSoundClouds.CountAsync(cancellationToken),
            sqlDatabaseContext.ParsedVimeos.CountAsync(cancellationToken),
            sqlDatabaseContext.ParsedYouTubes.CountAsync(cancellationToken));

        statitics.Vectors = Convert.ToInt32(vectors);

        statitics.ParsedTags = parsedTags;
        statitics.EmptyTags = emptyTags;
        statitics.ParsedPosts = parsedPosts;

        statitics.ParsedPostAttributePictures = parsedPostAttributePictures.Total;
        statitics.ParsedPostAttributePicturesNoContent = parsedPostAttributePictures.NoContent;
        statitics.ParsedPostAttributePicturesUnsupported = parsedPostAttributePictures.Unsupported;
        statitics.ParsedPostAttributePicturesWithVector = parsedPostAttributePictures.WithVector;
        statitics.ParsedPostAttributePicturesWithoutVector = parsedPostAttributePictures.WithoutVector;

        statitics.ParsedPostAttributeEmbeds = parsedPostAttributeEmbeds;

        statitics.ParsedBandCamps = parsedBandCamps;
        statitics.ParsedCoubs = parsedCoubs;
        statitics.ParsedSoundClouds = parsedSoundClouds;
        statitics.ParsedVimeos = parsedVimeos;
        statitics.ParsedYouTubes = parsedYouTubes;

        return statitics;
    }
}