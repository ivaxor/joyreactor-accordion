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
            .Where(ppap => ppap.AttributeId == context.Message.AttributeId)
            .FirstAsync(context.CancellationToken);

        if (picture.IsVectorCheckedForDuplicates == true)
            return;

        picture.IsVectorCheckedForDuplicates = true;
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

            var duplicatePictureIds = votes.Select(dpv => dpv.DuplicatePictureId).ToArray();
            await CleanUpVotesAsync(duplicatePictureIds, context.CancellationToken);
            await sqlDatabaseContext.SaveChangesAsync(context.CancellationToken);            

            var messages = duplicatePictureIds.Select(id => new VoteCreatedMessage() { DuplicatePictureId = id }).ToArray();
            await publishEndpoint.PublishBatch(messages, context.CancellationToken);
        }

        await transaction.CommitAsync(context.CancellationToken);
    }

    protected async Task<DuplicatePictureVote[]> CreateVotesAsync(ParsedPostAttributePicture picture, CancellationToken cancellationToken)
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
            scoreThreshold: 0.99f,
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

    protected async Task CleanUpVotesAsync(IEnumerable<Guid> duplicatePictureIds, CancellationToken cancellationToken)
    {
        if (!duplicatePictureIds.Any())
            return;

        // https://joyreactor.cc/tag/%D0%B1%D0%B0%D1%8F%D0%BD
        // Только посты после 15 ноября 2017 года могут быть баянами
        var beforeDuplicatePostThreshold = await sqlDatabaseContext.DuplicatePictureVotes
            .Where(v => duplicatePictureIds.Contains(v.DuplicatePictureId))
            .Where(v => v.VotingClosed == false)
            .Where(v => v.DuplicatePicture.Post.NumberId < 3302432)
            .ToArrayAsync(cancellationToken);

        var nearPosts = await sqlDatabaseContext.DuplicatePictureVotes
            .Where(v => duplicatePictureIds.Contains(v.DuplicatePictureId))
            .Where(v => v.VotingClosed == false)
            .Where(v => v.DuplicatePicture.Post.NumberId - v.OriginalPicture.Post.NumberId < 10)
            .ToArrayAsync(cancellationToken);

        var differentPictureCount = await sqlDatabaseContext.DuplicatePictureVotes
            .Where(v => duplicatePictureIds.Contains(v.DuplicatePictureId))
            .Where(v => v.VotingClosed == false)
            .Where(v => v.DuplicatePicture.Post.AttributePictures.Count > v.OriginalPicture.Post.AttributePictures.Count)
            .ToArrayAsync(cancellationToken);

        var differentPictureVoteCount = await sqlDatabaseContext.DuplicatePictureVotes
            .Where(v => duplicatePictureIds.Contains(v.DuplicatePictureId))
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
        }

        logger.LogDebug("Closed voting for {DuplicatesCount} vote(s) due to beign before duplicate post threshold.", beforeDuplicatePostThreshold.Length);
        logger.LogDebug("Closed voting for {DuplicatesCount} vote(s) due to near post ids.", nearPosts.Length);
        logger.LogDebug("Closed voting for {DuplicatesCount} vote(s) due to picture count difference.", differentPictureCount.Length);
        logger.LogDebug("Closed voting for {DuplicatesCount} vote(s) due to picture vote count difference.", differentPictureVoteCount.Length);
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