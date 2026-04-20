using JoyReactor.Accordion.Logic.Database.Sql;
using JoyReactor.Accordion.Logic.Database.Sql.Entities;
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
            .Include(dpv => dpv.OriginalPicture)
            .Include(dpv => dpv.DuplicatePicture)
            .Where(dpv => dpv.VotingClosed == false)
            .OrderBy(dpv => dpv.DuplicatePictureId)
            .ThenBy(dpv => dpv.OriginalPictureId)
            .Skip(PageSize * page)
            .Take(PageSize)
            .Select(dpv => new DuplicatePictureVoteExtended(
                dpv,
                dpv.OriginalPicture.Post.NumberId,
                dpv.OriginalPicture.Post.AttributePictures.Count,
                dpv.DuplicatePicture.Post.NumberId,
                dpv.DuplicatePicture.Post.AttributePictures.Count,
                dpv.DuplicatePicture.Post.Nsfw || dpv.OriginalPicture.Post.Nsfw))
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
        [FromQuery] int? duplicatePictureId,
        [FromQuery] DateTime? createdAt,
        CancellationToken cancellationToken = default)
    {
        var votesQuery = sqlDatabaseContext.DuplicatePictureVotes
            .AsNoTracking()
            .Where(dpv => dpv.VotingClosed == false);

        if (duplicatePictureId != null)
            votesQuery = votesQuery.Where(dpv => dpv.DuplicatePictureId > duplicatePictureId.Value.ToGuid());

        if (createdAt != null)
            votesQuery = votesQuery.Where(dpv => dpv.CreatedAt > createdAt);

        var votes = await votesQuery
            .Include(dpv => dpv.OriginalPicture)
            .Include(dpv => dpv.DuplicatePicture)
            .OrderBy(dpv => dpv.DuplicatePictureId)
            .ThenBy(dpv => dpv.OriginalPictureId)
            .Take(PageSize)
            .Select(dpv => new DuplicatePictureVoteExtended(
                dpv,
                dpv.OriginalPicture.Post.NumberId,
                dpv.OriginalPicture.Post.AttributePictures.Count,
                dpv.DuplicatePicture.Post.NumberId,
                dpv.DuplicatePicture.Post.AttributePictures.Count,
                dpv.DuplicatePicture.Post.Nsfw || dpv.OriginalPicture.Post.Nsfw))
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

    [HttpDelete]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CloseAllAsync(
        [FromQuery] int duplicatePostId,
        CancellationToken cancellationToken = default)
    {
        var votes = await sqlDatabaseContext.DuplicatePictureVotes
            .Where(dpv => dpv.DuplicatePicture.Post.NumberId == duplicatePostId)
            .Where(dpv => dpv.VotingClosed == false)
            .ToArrayAsync(cancellationToken);

        if (votes.Length == 0)
            return NotFound();

        foreach (var vote in votes)
        {
            vote.VotingClosed = true;
        }
        await sqlDatabaseContext.SaveChangesAsync(cancellationToken);

        return Ok();
    }
}