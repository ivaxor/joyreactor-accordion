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

        var sqlResults = await sqlDatabaseContext.Apis
            .GroupBy(_ => 1)
            .Select(g => new
            {
                ParsedTags = sqlDatabaseContext.ParsedTags.Count(),
                EmptyTags = sqlDatabaseContext.EmptyTags.Count(),

                ParsedPosts = sqlDatabaseContext.ParsedPosts.Count(),

                ParsedPostAttributePictures = sqlDatabaseContext.ParsedPostAttributePictures.Count(),
                ParsedPostAttributePicturesNoContent = sqlDatabaseContext.ParsedPostAttributePictures.Count(p => p.NoContent == true),
                ParsedPostAttributePicturesNoContentDueToDns = sqlDatabaseContext.ParsedPostAttributePictures.Count(p => p.NoContentDueToDns == true),
                ParsedPostAttributePicturesUnsupported = sqlDatabaseContext.ParsedPostAttributePictures.Count(p => p.UnsupportedContent == true),
                ParsedPostAttributePicturesWithVector = sqlDatabaseContext.ParsedPostAttributePictures.Count(p => p.IsVectorCreated == true),
                ParsedPostAttributePicturesWithoutVector = sqlDatabaseContext.ParsedPostAttributePictures.Count(p => p.IsVectorCreated == false && p.NoContent == false && p.NoContentDueToDns == false && p.UnsupportedContent == false),

                ParsedPostAttributeEmbeds = sqlDatabaseContext.ParsedPostAttributeEmbeds.Count(),

                ParsedBandCamps = sqlDatabaseContext.ParsedBandCamps.Count(),
                ParsedCoubs = sqlDatabaseContext.ParsedCoubs.Count(),
                ParsedSoundClouds = sqlDatabaseContext.ParsedSoundClouds.Count(),
                ParsedVimeos = sqlDatabaseContext.ParsedVimeos.Count(),
                ParsedYouTubes = sqlDatabaseContext.ParsedYouTubes.Count(),
            })
            .FirstAsync(cancellationToken);

        statitics.ParsedTags = sqlResults.ParsedTags;
        statitics.EmptyTags = sqlResults.EmptyTags;

        statitics.ParsedPosts = sqlResults.ParsedPosts;

        statitics.ParsedPostAttributePictures = sqlResults.ParsedPostAttributePictures;
        statitics.ParsedPostAttributePicturesNoContent = sqlResults.ParsedPostAttributePicturesNoContent;
        statitics.ParsedPostAttributePicturesNoContentDueToDns = sqlResults.ParsedPostAttributePicturesNoContentDueToDns;
        statitics.ParsedPostAttributePicturesUnsupported = sqlResults.ParsedPostAttributePicturesUnsupported;
        statitics.ParsedPostAttributePicturesWithVector = sqlResults.ParsedPostAttributePicturesWithVector;
        statitics.ParsedPostAttributePicturesWithoutVector = sqlResults.ParsedPostAttributePicturesWithoutVector;

        statitics.ParsedPostAttributeEmbeds = sqlResults.ParsedPostAttributeEmbeds;

        statitics.ParsedBandCamps = sqlResults.ParsedBandCamps;
        statitics.ParsedCoubs = sqlResults.ParsedCoubs;
        statitics.ParsedSoundClouds = sqlResults.ParsedSoundClouds;
        statitics.ParsedVimeos = sqlResults.ParsedVimeos;
        statitics.ParsedYouTubes = sqlResults.ParsedYouTubes;

        return statitics;
    }
}