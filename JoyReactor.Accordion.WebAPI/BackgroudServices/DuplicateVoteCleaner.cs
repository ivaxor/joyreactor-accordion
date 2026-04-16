using JoyReactor.Accordion.Logic.Database.Sql;
using JoyReactor.Accordion.Logic.Database.Sql.Entities;
using JoyReactor.Accordion.WebAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JoyReactor.Accordion.WebAPI.BackgroudServices;

public class DuplicateVoteCleaner(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<BackgroundServiceSettings> settings,
    ILogger<DuplicateVoteCleaner> logger)
    : RobustBackgroundService(settings, logger)
{
    protected override bool IsIndefinite => true;

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        await using var serviceScope = serviceScopeFactory.CreateAsyncScope();
        await using var sqlDatabaseContext = serviceScope.ServiceProvider.GetRequiredService<SqlDatabaseContext>();

        logger.LogInformation("Starting cleaning duplicate votes");

        // https://joyreactor.cc/tag/%D0%B1%D0%B0%D1%8F%D0%BD
        // Только посты после 15 ноября 2017 года могут быть баянами
        var beforeDuplicatePostThreshold = await sqlDatabaseContext.DuplicatePictureVotes
            .AsNoTracking()
            .Where(v => v.VotingClosed == false)
            .Where(v => v.DuplicatePicture.Post.NumberId < 3302432)
            .ToArrayAsync(cancellationToken);

        var nearPosts = await sqlDatabaseContext.DuplicatePictureVotes
            .AsNoTracking()
            .Where(v => v.VotingClosed == false)
            .Where(v => v.DuplicatePicture.Post.NumberId - v.OriginalPicture.Post.NumberId < 10)
            .ToArrayAsync(cancellationToken);

        var differentPictureCount = await sqlDatabaseContext.DuplicatePictureVotes
            .AsNoTracking()
            .Where(v => v.VotingClosed == false)
            .Where(v => v.DuplicatePicture.Post.AttributePictures.Count > v.OriginalPicture.Post.AttributePictures.Count)
            .ToArrayAsync(cancellationToken);

        var differentPictureVoteCount = await sqlDatabaseContext.DuplicatePictureVotes
            .AsNoTracking()
            .Where(v => v.VotingClosed == false)
            .Where(v => v.DuplicatePicture.Post.AttributePictures.All(p => p.IsVectorCheckedForDuplicates))
            .Where(v => v.DuplicatePicture.Post.AttributePictures.Any(p => p.VotesAsDuplicate.Count() == 0))
            .OrderBy(v => v.Id)
            .ToArrayAsync(cancellationToken);

        var votesToClose = Enumerable.Empty<DuplicatePictureVote>()
            .Concat(beforeDuplicatePostThreshold)
            .Concat(nearPosts)
            .Concat(differentPictureCount)
            .Concat(differentPictureVoteCount)
            .DistinctBy(v => v.Id)
            .ToArray();

        foreach (var voteToClose in votesToClose)
        {
            voteToClose.VotingClosed = true;
            voteToClose.UpdatedAt = DateTime.UtcNow;

            var entry = sqlDatabaseContext.Entry(voteToClose);
            entry.State = EntityState.Unchanged;
            entry.Property(p => p.VotingClosed).IsModified = true;
        }
        logger.LogInformation("Closed voting for {DuplicatesCount} vote(s) due to beign before duplicate post threshold.", beforeDuplicatePostThreshold.Length);
        logger.LogInformation("Closed voting for {DuplicatesCount} vote(s) due to near post ids.", nearPosts.Length);
        logger.LogInformation("Closed voting for {DuplicatesCount} vote(s) due to picture count difference.", differentPictureCount.Length);
        logger.LogInformation("Closed voting for {DuplicatesCount} vote(s) due to picture vote count difference.", differentPictureVoteCount.Length);

        await sqlDatabaseContext.SaveChangesAsync(cancellationToken);
        sqlDatabaseContext.ChangeTracker.Clear();
    }
}