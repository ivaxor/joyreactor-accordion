using JoyReactor.Accordion.Logic.BandCamp;
using JoyReactor.Accordion.Logic.Database.Sql;
using JoyReactor.Accordion.Logic.SoundCloud;
using JoyReactor.Accordion.WebAPI.Models.Requests;
using JoyReactor.Accordion.WebAPI.Models.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JoyReactor.Accordion.WebAPI.Controllers;

[Route("search/embedded")]
[ApiController]
[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
public class SearchEmbeddedController(
    IBandCampApiClient bandCampApiClient,
    ISoundCloudApiClient soundCloudApiClient,
    SqlDatabaseContext sqlDatabaseContext)
    : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType<SearchEmbeddedResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchAsync(
        [FromBody] SearchEmbeddedRequest request,
        CancellationToken cancellationToken)
    {
        var entityId = Guid.Empty;
        switch (request.Type)
        {
            case SearchEmbeddedType.BandCamp:
                var bandCampResponse = await bandCampApiClient.GetInfoAsync(request.Text, cancellationToken);
                if (bandCampResponse == null)
                    break;

                var type = bandCampResponse.Type switch
                {
                    "a" => "album",
                    "t" => "track",
                    _ => throw new NotImplementedException(),
                };

                entityId = await sqlDatabaseContext.ParsedBandCamps
                    .AsNoTracking()
                    .Where(bandCamp => bandCamp.UrlPath == $"{type}={bandCampResponse.Id}")
                    .Select(bc => bc.Id)
                    .FirstOrDefaultAsync(cancellationToken);
                break;

            case SearchEmbeddedType.Coub:
                entityId = await sqlDatabaseContext.ParsedCoubs
                    .AsNoTracking()
                    .Where(c => c.VideoId == request.Text)
                    .Select(c => c.Id)
                    .FirstOrDefaultAsync(cancellationToken);
                break;

            case SearchEmbeddedType.SoundCloud:
                var soundCloudResponse = await soundCloudApiClient.GetByPermaLinkAsync(request.Text, cancellationToken);
                if (soundCloudResponse == null)
                    break;

                entityId = await sqlDatabaseContext.ParsedSoundClouds
                    .AsNoTracking()
                    .Where(sc => sc.UrlPath == $"{soundCloudResponse.Kind}s/{soundCloudResponse.Id}")
                    .Select(sc => sc.Id)
                    .FirstOrDefaultAsync(cancellationToken);
                break;

            case SearchEmbeddedType.Vimeo:
                entityId = await sqlDatabaseContext.ParsedVimeos
                    .AsNoTracking()
                    .Where(v => v.VideoId == request.Text)
                    .Select(v => v.Id)
                    .FirstOrDefaultAsync(cancellationToken);
                break;

            case SearchEmbeddedType.YouTube:
                entityId = await sqlDatabaseContext.ParsedYouTubes
                    .AsNoTracking()
                    .Where(yt => yt.VideoId == request.Text)
                    .Select(yt => yt.Id)
                    .FirstOrDefaultAsync(cancellationToken);
                break;
        }
        if (entityId == Guid.Empty)
        {
            var emptyResponse = new SearchEmbeddedResponse(Enumerable.Empty<Guid>());
            return Ok(emptyResponse);
        }

        var query = sqlDatabaseContext.ParsedPostAttributeEmbeds.AsNoTracking();
        query = request.Type switch
        {
            SearchEmbeddedType.BandCamp => query.Where(ppae => ppae.BandCampId == entityId),
            SearchEmbeddedType.Coub => query.Where(ppae => ppae.CoubId == entityId),
            SearchEmbeddedType.SoundCloud => query.Where(ppae => ppae.SoundCloudId == entityId),
            SearchEmbeddedType.Vimeo => query.Where(ppae => ppae.VimeoId == entityId),
            SearchEmbeddedType.YouTube => query.Where(ppae => ppae.YouTubeId == entityId),
            _ => throw new NotImplementedException(),
        };

        var postIds = await query
            .OrderBy(ppae => ppae.PostId)
            .Select(ppae => ppae.PostId)
            .Take(request.Limit)
            .ToArrayAsync(cancellationToken);

        var response = new SearchEmbeddedResponse(postIds);
        return Ok(response);
    }
}