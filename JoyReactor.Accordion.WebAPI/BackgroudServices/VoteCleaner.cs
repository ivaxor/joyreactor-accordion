using JoyReactor.Accordion.Logic.Database.Sql;
using JoyReactor.Accordion.Logic.Database.Sql.Entities;
using JoyReactor.Accordion.WebAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JoyReactor.Accordion.WebAPI.BackgroudServices;

public class VoteCleaner(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<BackgroundServiceSettings> settings,
    ILogger<VoteCleaner> logger)
    : RobustBackgroundService(settings, logger)
{
    protected override bool IsIndefinite => true;
    protected override TimeSpan SubsequentRunDelay => TimeSpan.FromHours(1);

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        await using var serviceScope = serviceScopeFactory.CreateAsyncScope();
        await using var sqlDatabaseContext = serviceScope.ServiceProvider.GetRequiredService<SqlDatabaseContext>();

        // https://joyreactor.cc/tag/%D0%B1%D0%B0%D1%8F%D0%BD
        // Только посты после 15 ноября 2017 года могут быть баянами
        var beforeThresholdVotes = await sqlDatabaseContext.DuplicatePictureVotes
            .AsNoTracking()
            .Where(v => v.VotingClosed == false)
            .Where(v => v.DuplicatePicture.Post.NumberId < 3302432)
            .ToArrayAsync(cancellationToken);

        var nearVotes = await sqlDatabaseContext.DuplicatePictureVotes
            .AsNoTracking()
            .Where(v => v.VotingClosed == false)
            .Where(v => v.DuplicatePicture.Post.NumberId - v.OriginalPicture.Post.NumberId < 10)
            .ToArrayAsync(cancellationToken);

        var morePicturesVotes = await sqlDatabaseContext.DuplicatePictureVotes
            .AsNoTracking()
            .Where(v => v.VotingClosed == false)
            .Where(v => v.DuplicatePicture.Post.AttributePictures.Count > v.OriginalPicture.Post.AttributePictures.Count)
            .ToArrayAsync(cancellationToken);

        DuplicatePictureVote[] votesToClose = [.. beforeThresholdVotes, .. nearVotes, .. morePicturesVotes];
        foreach (var voteToClose in votesToClose)
        {
            voteToClose.VotingClosed = true;
            voteToClose.UpdatedAt = DateTime.UtcNow;

            var entry = sqlDatabaseContext.Entry(voteToClose);
            entry.State = EntityState.Unchanged;
            entry.Property(p => p.VotingClosed).IsModified = true;
        }
        logger.LogInformation("Closed voting for {DuplicatesCount} before post threshold vote(s).", beforeThresholdVotes.Length);
        logger.LogInformation("Closed voting for {DuplicatesCount} near post vote(s).", nearVotes.Length);
        logger.LogInformation("Closed voting for {DuplicatesCount} more post picture vote(s).", morePicturesVotes.Length);

        //await sqlDatabaseContext.SaveChangesAsync(cancellationToken);
        sqlDatabaseContext.ChangeTracker.Clear();
    }
}