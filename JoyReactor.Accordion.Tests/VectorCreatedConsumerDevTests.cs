using JoyReactor.Accordion.Tests.Helpers;
using JoyReactor.Accordion.WebAPI.Consumers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;

namespace JoyReactor.Accordion.Tests;

#if DEBUG
[TestClass]
#endif
public class VectorCreatedConsumerDevTests
{
    protected IHost Host { get; set; }
    protected TestDependencyProvider DependencyProvider { get; set; }

    [TestInitialize]
    public async Task TestInitializeAsync()
    {
        Host = TestHostApplicationBuilder.CreateReal().Build();
        DependencyProvider = new TestDependencyProvider(Host.Services);
    }

    [TestCleanup]
    public async Task TestCleanupAsync()
    {
        Host.Dispose();
    }

    [TestMethod]
    public async Task CreateVotesAsync()
    {
        var sqlDatabaseContext = DependencyProvider.SqlDatabaseContext;
        await using var transaction = await sqlDatabaseContext.Database.BeginTransactionAsync();

        var vectorCreatedConsumer = new VectorCreatedConsumer(
            sqlDatabaseContext,
            DependencyProvider.QdrantClient,
            null,
            DependencyProvider.QdrantSettings,
            NullLogger<VectorCreatedConsumer>.Instance);

        var picture = await sqlDatabaseContext.ParsedPostAttributePictures
            .AsNoTracking()
            .Where(ppap => ppap.Post.NumberId == 6311396)
            .FirstOrDefaultAsync();

        var votes = await vectorCreatedConsumer.CreateVotesAsync(picture, default);

        await transaction.RollbackAsync();
    }

    [TestMethod]
    public async Task GetVoteToCloseUpAsync()
    {
        var sqlDatabaseContext = DependencyProvider.SqlDatabaseContext;
        await using var transaction = await sqlDatabaseContext.Database.BeginTransactionAsync();

        Console.WriteLine("Test1");
        Debug.WriteLine("Test2");

        var vectorCreatedConsumer = new VectorCreatedConsumer(
            sqlDatabaseContext,
            DependencyProvider.QdrantClient,
            null,
            DependencyProvider.QdrantSettings,
            NullLogger<VectorCreatedConsumer>.Instance);

        var vote = await sqlDatabaseContext.DuplicatePictureVotes
            .AsNoTracking()
            .Include(dpv => dpv.DuplicatePicture)
            .Where(dpv => dpv.DuplicatePicture.Post.NumberId == 6269750)
            .FirstOrDefaultAsync();

        var picture = vote.DuplicatePicture;
        var cancellationToken = CancellationToken.None;

        #region Partial coverage
        // Calculate amount of pictures in duplicate post
        var duplicatePostPictureCount = await sqlDatabaseContext.ParsedPostAttributePictures
            .Where(ppap => ppap.PostId == picture.PostId)
            .CountAsync(cancellationToken);

        // Get original post ids that are partially covered by duplicate post
        // If amount of votes for any original post is not equal to duplicate post picture count, then it's not a full coverage
        var partialCoverageOriginalPostIds = sqlDatabaseContext.DuplicatePictureVotes
            .Where(v => v.DuplicatePicture.PostId == picture.PostId)
            .GroupBy(v => v.OriginalPicture.PostId)
            .Where(g => g.Select(x => x.DuplicatePictureId).Distinct().Count() != duplicatePostPictureCount)
            .Select(g => g.Key);

        var partialCoverage = await sqlDatabaseContext.DuplicatePictureVotes
            .AsNoTracking()
            .Where(v => v.DuplicatePicture.PostId == picture.PostId)
            .Where(v => v.VotingClosed == false)
            .Where(v => v.DuplicatePicture.Post.AttributePictures.All(p => p.IsVectorCheckedForDuplicates))
            .Where(v => partialCoverageOriginalPostIds.Contains(v.OriginalPicture.PostId))
            .ToArrayAsync(cancellationToken);
        #endregion

        var voteToClose = await vectorCreatedConsumer.GetVoteToCloseUpAsync(vote.DuplicatePicture, default);

        await transaction.RollbackAsync();
    }

    [TestMethod]
    public async Task CleanUpVotesAsync()
    {
        var differentPictureVoteCount = await DependencyProvider.SqlDatabaseContext.DuplicatePictureVotes
            .AsNoTracking()
            .Where(v => v.VotingClosed == false)
            .Where(v => v.DuplicatePicture.Post.AttributePictures.All(p => p.IsVectorCheckedForDuplicates))
            .Where(v => v.DuplicatePicture.Post.AttributePictures.Any(p => p.VotesAsDuplicate.Count() == 0))
            .OrderBy(v => v.Id)
            .ToArrayAsync();

        DependencyProvider.SqlDatabaseContext.DuplicatePictureVotes.RemoveRange(differentPictureVoteCount);
        await DependencyProvider.SqlDatabaseContext.SaveChangesAsync();
    }
}