using JoyReactor.Accordion.Logic.Database.Sql;
using JoyReactor.Accordion.Logic.Database.Sql.Entities;
using JoyReactor.Accordion.Logic.Database.Vector;
using JoyReactor.Accordion.Logic.Database.Vector.Entities;
using JoyReactor.Accordion.Logic.Extensions;
using JoyReactor.Accordion.Logic.MQ.Messages;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace JoyReactor.Accordion.WebAPI.Consumers;

public class VectorCreatedConsumer(
    SqlDatabaseContext sqlDatabaseContext,
    IQdrantClient qdrantClient,
    IPublishEndpoint publishEndpoint,
    IOptions<QdrantSettings> qdrantSettings,
    ILogger<VectorCreatedConsumer> logger)
    : IConsumer<VectorCreatedMessage>
{
    public async Task Consume(ConsumeContext<VectorCreatedMessage> context)
    {
        await using var transaction = await sqlDatabaseContext.Database.BeginTransactionAsync(context.CancellationToken);

        var picture = await sqlDatabaseContext.ParsedPostAttributePictures
            .AsNoTracking()
            .Where(ppap => ppap.AttributeId == context.Message.AttributeId)
            .FirstAsync(context.CancellationToken);

        if (picture.IsVectorCreated == false)
        {
            logger.LogWarning("Tried to check {PictureAttibuteId} post picture attribute without vector for duplicates.", picture.AttributeId);
            return;
        }

        if (picture.IsVectorCheckedForDuplicates == true)
            return;

        picture.IsVectorCheckedForDuplicates = true;
        picture.UpdatedAt = DateTime.UtcNow;

        var pictureEntry = sqlDatabaseContext.ParsedPostAttributePictures.Entry(picture);
        pictureEntry.Property(e => e.IsVectorCheckedForDuplicates).IsModified = true;
        pictureEntry.Property(e => e.UpdatedAt).IsModified = true;

        await sqlDatabaseContext.SaveChangesAsync(context.CancellationToken);

        var votes = await CreateVotesAsync(picture, context.CancellationToken);
        if (votes.Length != 0)
        {
            var votesUpserted = await sqlDatabaseContext.DuplicatePictureVotes
                .UpsertRange(votes)
                .On(dpv => new { dpv.OriginalPictureId, dpv.DuplicatePictureId })
                .NoUpdate()
                .RunAsync(context.CancellationToken);
            await sqlDatabaseContext.SaveChangesAsync(context.CancellationToken);
            logger.LogDebug("Upserted {DuplicatesCount} post attribute picture duplicate vote(s) for {PictureAttributeId}.", votesUpserted, picture.AttributeId);

            var messages = votes.Select(dpv => new VoteCreatedMessage() { DuplicatePictureId = dpv.DuplicatePictureId }).ToArray();
            await publishEndpoint.PublishBatch(messages, context.CancellationToken);
        }

        var votesToClose = await GetVoteToCloseUpAsync(picture, context.CancellationToken);
        foreach (var voteToClose in votesToClose)
        {
            voteToClose.VotingClosed = true;
            voteToClose.UpdatedAt = DateTime.UtcNow;

            var voteToCloseEntry = sqlDatabaseContext.DuplicatePictureVotes.Entry(voteToClose);
            voteToCloseEntry.Property(e => e.VotingClosed).IsModified = true;
            voteToCloseEntry.Property(e => e.UpdatedAt).IsModified = true;
        }
        await sqlDatabaseContext.SaveChangesAsync(context.CancellationToken);

        await transaction.CommitAsync(context.CancellationToken);
    }

    internal async Task<DuplicatePictureVote[]> CreateVotesAsync(ParsedPostAttributePicture picture, CancellationToken cancellationToken)
    {
        // https://joyreactor.cc/tag/%D0%B1%D0%B0%D1%8F%D0%BD
        // Только посты после 15 ноября 2017 года могут быть баянами
        if (picture.PostId.ToInt() < 3302432)
            return [];

        var response = await qdrantClient.ScrollAsync(
            qdrantSettings.Value.CollectionName,
            filter: new Filter()
            {
                Must =
                {
                    new Condition()
                    {
                        Field = new FieldCondition()
                        {
                            Key = "postAttributeId",
                            Match = new Match() { Integer = picture.AttributeId }
                        },
                    },
                },
            },
            vectorsSelector: new WithVectorsSelector() { Enable = false },
            payloadSelector: new WithPayloadSelector() { Enable = true },
            cancellationToken: cancellationToken);

        if (response.Result.Count == 0)
            logger.LogError("Failed to find vector for {PictureAttributeId} post attribute picture.", picture.AttributeId);

        var initialPoint = response.Result
            .Select(v => new PictureRetrivedPoint(v))
            .OrderBy(p => p.ContentVersion)
            .Last();

        var similarVectors = await qdrantClient.RecommendAsync(
            qdrantSettings.Value.CollectionName,
            positive: [initialPoint.PointId],
            filter: new Filter()
            {
                MustNot =
                {
                    new Condition()
                    {
                        Field = new FieldCondition()
                        {
                            Key = "postId",
                            Match = new Match() { Integer = initialPoint.PostId.Value }
                        }
                    },
                    new Condition()
                    {
                        Field = new FieldCondition()
                        {
                            Key = "postAttributeId",
                            Match = new Match() { Integer = initialPoint.PostAttributeId.Value }
                        }
                    },
                }
            },
            scoreThreshold: 0.96f,
            limit: 25,
            vectorsSelector: new WithVectorsSelector() { Enable = false },
            payloadSelector: new WithPayloadSelector() { Enable = true },
            cancellationToken: cancellationToken);

        if (similarVectors.Count == 0)
            return [];

        var similarPoints = similarVectors
            .Select(v => new PictureScoredPoint(v))
            .Where(p => p.PostId != initialPoint.PostId)
            .Where(p => p.PostAttributeId != initialPoint.PostAttributeId)
            .Where(p => Math.Abs(p.PostId.Value - initialPoint.PostId.Value) >= 10)
            .GroupBy(p => p.PostId, (key, collection) => collection.OrderByDescending(e => e.ContentVersion).First())
            .ToArray();

        var similarPointPostAttributeIds = similarPoints
            .Select(p => p.PostAttributeId)
            .ToArray();

        var existingSimilarPointPostAttributeIds = await sqlDatabaseContext.ParsedPostAttributePictures
            .AsNoTracking()
            .Where(p => similarPointPostAttributeIds.Contains(p.AttributeId))
            .Select(p => p.AttributeId)
            .ToArrayAsync(cancellationToken);

        if (similarPointPostAttributeIds.Length != existingSimilarPointPostAttributeIds.Length)
        {
            var missingSimilarPoints = similarPointPostAttributeIds.Length - existingSimilarPointPostAttributeIds.Length;
            logger.LogWarning("{PointCount} similar point(s) no longer exists in SQL database.", missingSimilarPoints);
        }

        var votes = similarPoints
            .Where(similarPoint => existingSimilarPointPostAttributeIds.Contains(similarPoint.PostAttributeId.Value))
            .Select(similarPoint => initialPoint.PostId < similarPoint.PostId
                ? new DuplicatePictureVote(initialPoint, similarPoint)
                : new DuplicatePictureVote(similarPoint, initialPoint))
            .ToArray();

        logger.LogDebug("Found {DuplicatesCount} post attribute picture duplicate(s) for {PictureAttributeId}.", similarPoints.Length, picture.AttributeId);

        return votes;
    }

    internal async Task<DuplicatePictureVote[]> GetVoteToCloseUpAsync(ParsedPostAttributePicture picture, CancellationToken cancellationToken)
    {
        // https://joyreactor.cc/tag/%D0%B1%D0%B0%D1%8F%D0%BD
        // Только посты после 15 ноября 2017 года могут быть баянами
        var beforeDuplicatePostThreshold = await sqlDatabaseContext.DuplicatePictureVotes
            .AsNoTracking()
            .Where(v => v.DuplicatePicture.PostId == picture.PostId)
            .Where(v => v.VotingClosed == false)
            .Where(v => v.DuplicatePicture.Post.NumberId < 3302432)
            .ToArrayAsync(cancellationToken);

        var nearPosts = await sqlDatabaseContext.DuplicatePictureVotes
            .AsNoTracking()
            .Where(v => v.DuplicatePicture.PostId == picture.PostId)
            .Where(v => v.VotingClosed == false)
            .Where(v => v.DuplicatePicture.Post.NumberId - v.OriginalPicture.Post.NumberId < 10)
            .ToArrayAsync(cancellationToken);

        var differentPictureCount = await sqlDatabaseContext.DuplicatePictureVotes
            .AsNoTracking()
            .Where(v => v.DuplicatePicture.PostId == picture.PostId)
            .Where(v => v.VotingClosed == false)
            .Where(v => v.DuplicatePicture.Post.AttributePictures.Count > v.OriginalPicture.Post.AttributePictures.Count)
            .ToArrayAsync(cancellationToken);

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

        var votesToClose = Enumerable.Empty<DuplicatePictureVote>()
            .Concat(beforeDuplicatePostThreshold)
            .Concat(nearPosts)
            .Concat(differentPictureCount)
            .Concat(partialCoverage)
            .DistinctBy(v => v.Id)
            .ToArray();

        logger.LogDebug("Found {DuplicatesCount} vote(s) before before duplicate post threshold.", beforeDuplicatePostThreshold.Length);
        logger.LogDebug("Found {DuplicatesCount} vote(s) with near post ids.", nearPosts.Length);
        logger.LogDebug("Found {DuplicatesCount} vote(s) with picture count difference.", differentPictureCount.Length);
        logger.LogDebug("Found {DuplicatesCount} vote(s) with partial coverage.", partialCoverage.Length);

        return votesToClose;
    }
}

public class VectorCreatedConsumerDefinition : ConsumerDefinition<VectorCreatedConsumer>
{
    public VectorCreatedConsumerDefinition()
    {
        EndpointName = "vector_created";
        ConcurrentMessageLimit = 5;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<VectorCreatedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retryConfurator => retryConfurator.Interval(3, TimeSpan.FromSeconds(5)));
    }
}