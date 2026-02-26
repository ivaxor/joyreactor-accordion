using JoyReactor.Accordion.Logic.Database.Sql;
using JoyReactor.Accordion.Logic.Database.Sql.Entities;
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
    protected static readonly SemaphoreSlim VoteSemaphore = new SemaphoreSlim(1, 1);

    [HttpGet("pager")]
    [AllowAnonymous]
    [ProducesResponseType<DuplicatePictureVoteThinResponse[]>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(
        [FromQuery] int page = 0,
        CancellationToken cancellationToken = default)
    {
        const int pageSize = 100;

        var votes = await sqlDatabaseContext.DuplicatePictureVotes
            .AsNoTracking()
            .Include(dpv => dpv.OriginalPicture)
            .Include(dpv => dpv.DuplicatePictureId)
            .OrderBy(dpv => dpv.CreatedAt)
            .Skip(pageSize * page)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

        var votesThin = votes
            .Select(v => new DuplicatePictureVoteThinResponse(v))
            .ToArray();

        return Ok(votesThin);
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<DuplicatePictureVoteThinResponse[]>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(
        [FromQuery] DateTime createdAfter,
        CancellationToken cancellationToken = default)
    {
        var votes = await sqlDatabaseContext.DuplicatePictureVotes
            .AsNoTracking()
            .Where(dpv => dpv.VotingClosed == false)
            .Where(dpv => dpv.CreatedAt > createdAfter)
            .Include(dpv => dpv.OriginalPicture)
            .Include(dpv => dpv.DuplicatePictureId)
            .OrderBy(dpv => dpv.CreatedAt)
            .Take(10)
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