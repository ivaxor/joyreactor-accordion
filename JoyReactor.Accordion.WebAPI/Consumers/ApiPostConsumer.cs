using JoyReactor.Accordion.Logic.ApiClient.Models;
using JoyReactor.Accordion.Logic.Database.Sql;
using JoyReactor.Accordion.Logic.Database.Sql.Entities;
using JoyReactor.Accordion.Logic.MQ.Messages;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace JoyReactor.Accordion.WebAPI.Consumers;

public class ApiPostConsumer(SqlDatabaseContext sqlDatabaseContext)
    : IConsumer<ApiPostMessage>
{
    public async Task Consume(ConsumeContext<ApiPostMessage> context)
    {
        var parsedPost = await sqlDatabaseContext.ParsedPosts
            .Include(pp => pp.AttributePictures)
            .Include(pp => pp.AttributeEmbeds)
            .ThenInclude(ppae => ppae.BandCamp)
            .Include(pp => pp.AttributeEmbeds)
            .ThenInclude(ppae => ppae.Coub)
            .Include(pp => pp.AttributeEmbeds)
            .ThenInclude(ppae => ppae.SoundCloud)
            .Include(pp => pp.AttributeEmbeds)
            .ThenInclude(ppae => ppae.Vimeo)
            .Include(pp => pp.AttributeEmbeds)
            .ThenInclude(ppae => ppae.YouTube)
            .Where(pp => pp.NumberId == context.Message.Post.NumberId)
            .FirstOrDefaultAsync(context.CancellationToken);

        if (parsedPost != null)
            await UpdateAsync(parsedPost, context.Message, context.CancellationToken);
        else
            await CreateAsync(context.Message, context.CancellationToken);
    }

    protected async Task UpdateAsync(
        ParsedPost parsedPost,
        ApiPostMessage message,
        CancellationToken cancellationToken)
    {
        if (message.Post.ContentVersion <= parsedPost.ContentVersion)
            return;

        await using var transaction = await sqlDatabaseContext.Database.BeginTransactionAsync(cancellationToken);

        parsedPost.ContentVersion = message.Post.ContentVersion.Value;
        parsedPost.UpdatedAt = DateTime.UtcNow;

        var existingPostAttributePictures = parsedPost.AttributePictures
            .Select(ppap => ppap.AttributeId)
            .ToHashSet();
        var existingPostAttributeEmbeds = parsedPost.AttributeEmbeds
            .Select(ppae => ppae.UniqueId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var newPostAttributePictureIds = new HashSet<int>();
        var newPostAttributeEmbeddedIds = new HashSet<string>();

        foreach (var postAttribute in message.Post.Attributes)
        {
            switch (postAttribute.Type)
            {
                case "PICTURE":
                    newPostAttributePictureIds.Add(postAttribute.NumberId);
                    if (existingPostAttributePictures.Contains(postAttribute.NumberId))
                        continue;

                    var postAttributePicture = new ParsedPostAttributePicture(postAttribute, parsedPost);
                    await sqlDatabaseContext.ParsedPostAttributePictures.AddAsync(postAttributePicture, cancellationToken);
                    break;

                case "BANDCAMP":
                case "COUB":
                case "SOUNDCLOUD":
                case "VIMEO":
                case "YOUTUBE":
                    var parsedAttributeEmbedded = ParseAttributeEmbedded(postAttribute);
                    newPostAttributeEmbeddedIds.Add(parsedAttributeEmbedded.UniqueId);
                    if (existingPostAttributeEmbeds.Contains(parsedAttributeEmbedded.UniqueId))
                        continue;

                    var existingAttributeEmbedded = await TryToGetAttributeEmbeddedAsync(parsedAttributeEmbedded, cancellationToken);
                    if (existingAttributeEmbedded == null)
                        await AddAttributeEmbeddedAsync(parsedAttributeEmbedded, cancellationToken);

                    var parsedPostAttributeEmbedded = new ParsedPostAttributeEmbedded(postAttribute, parsedPost, existingAttributeEmbedded ?? parsedAttributeEmbedded);
                    await sqlDatabaseContext.ParsedPostAttributeEmbeds.AddAsync(parsedPostAttributeEmbedded, cancellationToken);
                    break;

                default:
                    throw new NotImplementedException();
            }
            await sqlDatabaseContext.SaveChangesAsync(cancellationToken);
        }

        foreach (var ppap in parsedPost.AttributePictures)
        {
            if (!newPostAttributePictureIds.Contains(ppap.AttributeId))
                sqlDatabaseContext.ParsedPostAttributePictures.Remove(ppap);
        }
        await sqlDatabaseContext.SaveChangesAsync(cancellationToken);

        foreach (var ppae in parsedPost.AttributeEmbeds)
        {
            if (!newPostAttributeEmbeddedIds.Contains(ppae.UniqueId))
                sqlDatabaseContext.ParsedPostAttributeEmbeds.Remove(ppae);
        }
        await sqlDatabaseContext.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    protected async Task CreateAsync(ApiPostMessage message, CancellationToken cancellationToken)
    {
        await using var transaction = await sqlDatabaseContext.Database.BeginTransactionAsync(cancellationToken);

        var parsedPost = new ParsedPost(message);
        await sqlDatabaseContext.ParsedPosts.AddAsync(parsedPost, cancellationToken);
        await sqlDatabaseContext.SaveChangesAsync(cancellationToken);

        foreach (var postAttribute in message.Post.Attributes)
        {
            switch (postAttribute.Type)
            {
                case "PICTURE":
                    var postAttributePicture = new ParsedPostAttributePicture(postAttribute, parsedPost);
                    await sqlDatabaseContext.ParsedPostAttributePictures.AddAsync(postAttributePicture, cancellationToken);
                    break;

                case "BANDCAMP":
                case "COUB":
                case "SOUNDCLOUD":
                case "VIMEO":
                case "YOUTUBE":
                    var parsedAttributeEmbedded = ParseAttributeEmbedded(postAttribute);
                    var existingAttributeEmbedded = await TryToGetAttributeEmbeddedAsync(parsedAttributeEmbedded, cancellationToken);

                    if (existingAttributeEmbedded == null)
                        await AddAttributeEmbeddedAsync(parsedAttributeEmbedded, cancellationToken);

                    var parsedPostAttributeEmbedded = new ParsedPostAttributeEmbedded(postAttribute, parsedPost, existingAttributeEmbedded ?? parsedAttributeEmbedded);
                    await sqlDatabaseContext.ParsedPostAttributeEmbeds.AddAsync(parsedPostAttributeEmbedded, cancellationToken);
                    break;

                default:
                    throw new NotImplementedException();
            }

            await sqlDatabaseContext.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    protected static IParsedAttributeEmbedded ParseAttributeEmbedded(PostAttribute postAttribute)
    {
        return postAttribute.Type switch
        {
            "BANDCAMP" => new ParsedBandCamp(postAttribute),
            "COUB" => new ParsedCoub(postAttribute),
            "SOUNDCLOUD" => new ParsedSoundCloud(postAttribute),
            "VIMEO" => new ParsedVimeo(postAttribute),
            "YOUTUBE" => new ParsedYouTube(postAttribute),
            _ => throw new NotImplementedException(),
        };
    }

    protected async Task<IParsedAttributeEmbedded?> TryToGetAttributeEmbeddedAsync(IParsedAttributeEmbedded parsedAttributeEmbedded, CancellationToken cancellationToken)
    {
        return parsedAttributeEmbedded switch
        {
            ParsedBandCamp parsedBandCamp => await sqlDatabaseContext.ParsedBandCamps.Where(bandCamp => bandCamp.UrlPath == parsedBandCamp.UrlPath).FirstOrDefaultAsync(cancellationToken),
            ParsedCoub parsedCoub => await sqlDatabaseContext.ParsedCoubs.Where(coub => coub.VideoId == parsedCoub.VideoId).FirstOrDefaultAsync(cancellationToken),
            ParsedSoundCloud parsedSoundCloud => await sqlDatabaseContext.ParsedSoundClouds.Where(soundCloud => soundCloud.UrlPath == parsedSoundCloud.UrlPath).FirstOrDefaultAsync(cancellationToken),
            ParsedVimeo parsedVimeo => await sqlDatabaseContext.ParsedVimeos.Where(vimeo => vimeo.VideoId == parsedVimeo.VideoId).FirstOrDefaultAsync(cancellationToken),
            ParsedYouTube parsedYouTube => await sqlDatabaseContext.ParsedYouTubes.Where(youTube => youTube.VideoId == parsedYouTube.VideoId).FirstOrDefaultAsync(cancellationToken),
            _ => throw new NotImplementedException(),
        };
    }

    protected async ValueTask AddAttributeEmbeddedAsync(IParsedAttributeEmbedded parsedAttributeEmbedded, CancellationToken cancellationToken)
    {
        switch (parsedAttributeEmbedded)
        {
            case ParsedBandCamp parsedBandCamp:
                await sqlDatabaseContext.ParsedBandCamps.AddAsync(parsedBandCamp, cancellationToken);
                break;

            case ParsedCoub parsedCoub:
                await sqlDatabaseContext.ParsedCoubs.AddAsync(parsedCoub, cancellationToken);
                break;

            case ParsedSoundCloud parsedSoundCloud:
                await sqlDatabaseContext.ParsedSoundClouds.AddAsync(parsedSoundCloud, cancellationToken);
                break;

            case ParsedVimeo parsedVimeo:
                await sqlDatabaseContext.ParsedVimeos.AddAsync(parsedVimeo, cancellationToken);
                break;

            case ParsedYouTube parsedYouTube:
                await sqlDatabaseContext.ParsedYouTubes.AddAsync(parsedYouTube, cancellationToken);
                break;

            default:
                throw new NotImplementedException();
        }
    }
}

public class ApiPostConsumerDefinition : ConsumerDefinition<ApiPostConsumer>
{
    public ApiPostConsumerDefinition(IReceiveEndpointConfigurator endpointConfigurator)
    {
        EndpointName = "api_post";
        ConcurrentMessageLimit = 1;

        endpointConfigurator.UseMessageRetry(retryConfurator => retryConfurator.Ignore());
    }
}