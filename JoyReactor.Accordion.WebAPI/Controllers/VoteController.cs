using JoyReactor.Accordion.Logic.Database.Sql;
using JoyReactor.Accordion.Logic.Extensions;
using JoyReactor.Accordion.WebAPI.Models.Requests;
using JoyReactor.Accordion.WebAPI.Models.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JoyReactor.Accordion.WebAPI.Controllers;

[Route("vote")]
[ApiController]
[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
public class VoteController(SqlDatabaseContext sqlDatabaseContext) : ControllerBase
{
    protected const int PageSize = 100;
    protected static readonly SemaphoreSlim VoteSemaphore = new SemaphoreSlim(1, 1);

    [HttpGet("pager")]
    [AllowAnonymous]
    [ProducesResponseType<PagedResponse<DuplicatePictureVoteThinResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(
        [FromQuery] int page = 0,
        CancellationToken cancellationToken = default)
    {
        var votes = await sqlDatabaseContext.DuplicatePictureVotes
            .AsNoTracking()
            .Where(dpv => dpv.VotingClosed == false)
            .Include(dpv => dpv.OriginalPicture)
            .Include(dpv => dpv.DuplicatePicture)
            .OrderBy(dpv => dpv.OriginalPictureId)
            .ThenBy(dpv => dpv.DuplicatePictureId)
            .Skip(PageSize * page)
            .Take(PageSize)
            .ToArrayAsync(cancellationToken);

        var votesThin = votes
            .Select(v => new DuplicatePictureVoteThinResponse(v))
            .ToArray();

        var votesTotal = await sqlDatabaseContext.DuplicatePictureVotes
            .AsNoTracking()
            .Where(dpv => dpv.VotingClosed == false)
            .CountAsync(cancellationToken);

        var paged = new PagedResponse<DuplicatePictureVoteThinResponse>()
        {
            Values = votesThin,
            Pages = Convert.ToInt32(Math.Ceiling((decimal)votesTotal / PageSize)),
        };

        return Ok(paged);
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<DuplicatePictureVoteThinResponse[]>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(
        [FromQuery] int? originalPictureId,
        [FromQuery] DateTime? createdAt,
        CancellationToken cancellationToken = default)
    {
        var votesQuery = sqlDatabaseContext.DuplicatePictureVotes
            .AsNoTracking()
            .Where(dpv => dpv.VotingClosed == false);

        if (originalPictureId != null)
            votesQuery = votesQuery.Where(dpv => dpv.OriginalPictureId > originalPictureId.Value.ToGuid());

        if (createdAt != null)
            votesQuery = votesQuery.Where(dpv => dpv.CreatedAt > createdAt);

        var votes = await votesQuery
            .Include(dpv => dpv.OriginalPicture)
            .Include(dpv => dpv.DuplicatePicture)
            .OrderBy(dpv => dpv.OriginalPictureId)
            .ThenBy(dpv => dpv.DuplicatePictureId)
            .Take(PageSize)
            .ToArrayAsync(cancellationToken);

        var votesThin = votes
            .Select(v => new DuplicatePictureVoteThinResponse(v))
            .ToArray();

        return Ok(votesThin);
    }

    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> VoteAsync(
        [FromBody] DuplicatePictureVoteRequest request,
        CancellationToken cancellationToken = default)
    {
        await VoteSemaphore.WaitAsync(cancellationToken);
        try
        {
            var vote = await sqlDatabaseContext.DuplicatePictureVotes
                .Where(dpv => dpv.VotingClosed == false)
                .Where(dpv => dpv.Id == request.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (vote == null)
                return NotFound();

            var voterIpAddress = HttpContext.Connection.RemoteIpAddress!.ToString();

            if (vote.YesVotes.Contains(voterIpAddress, StringComparer.OrdinalIgnoreCase) || vote.NoVotes.Contains(voterIpAddress, StringComparer.OrdinalIgnoreCase))
                return Conflict();

            if (request.Yes)
                vote.YesVotes = [.. vote.YesVotes, voterIpAddress];
            else
                vote.NoVotes = [.. vote.NoVotes, voterIpAddress];

            await sqlDatabaseContext.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            VoteSemaphore.Release();
        }

        return Ok();
    }

    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CloseAsync(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var vote = await sqlDatabaseContext.DuplicatePictureVotes
            .Where(dpv => dpv.Id == id)
            .FirstOrDefaultAsync(cancellationToken);

        if (vote == null)
            return NotFound();

        if (vote.VotingClosed)
            return Conflict();

        vote.VotingClosed = true;
        await sqlDatabaseContext.SaveChangesAsync(cancellationToken);

        return Ok();
    }
}