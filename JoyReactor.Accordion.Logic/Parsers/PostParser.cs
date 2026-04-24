using JoyReactor.Accordion.Logic.ApiClient.Models;
using JoyReactor.Accordion.Logic.Database.Sql;
using JoyReactor.Accordion.Logic.Database.Sql.Entities;
using JoyReactor.Accordion.Logic.MQ.Messages;
using Microsoft.EntityFrameworkCore;

namespace JoyReactor.Accordion.Logic.Parsers;

public class PostParser(SqlDatabaseContext sqlDatabaseContext)
    : IPostParser
{
    public Task ParseAsync(ApiPostMessage message, CancellationToken cancellationToken)
    {
        return ParseAsync(message.ApiId, message.Post, cancellationToken);
    }

    public async Task ParseAsync(Guid apiId, Post post, CancellationToken cancellationToken)
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
            .AsSplitQuery()
            .Where(pp => pp.NumberId == post.NumberId)
            .FirstOrDefaultAsync(cancellationToken);

        if (parsedPost != null)
            await UpdateAsync(parsedPost, apiId, post, cancellationToken);
        else
            await CreateAsync(apiId, post, cancellationToken);
    }

    protected async Task UpdateAsync(
        ParsedPost parsedPost,
        Guid apiId,
        Post post,
        CancellationToken cancellationToken)
    {
        if (post.ContentVersion <= parsedPost.ContentVersion)
            return;

        await using var transaction = await sqlDatabaseContext.Database.BeginTransactionAsync(cancellationToken);

        parsedPost.ContentVersion = post.ContentVersion.Value;
        parsedPost.UpdatedAt = DateTime.UtcNow;

        var existingPostAttributePictures = parsedPost.AttributePictures
            .Select(ppap => ppap.AttributeId)
            .ToHashSet();
        var existingPostAttributeEmbeds = parsedPost.AttributeEmbeds
            .Select(ppae => ppae.UniqueId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var newPostAttributePictureIds = new HashSet<int>();
        var newPostAttributeEmbeddedIds = new HashSet<string>();

        foreach (var postAttribute in post.Attributes)
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

        if (parsedPost.AttributePictures.Any())
        {
            foreach (var ppap in parsedPost.AttributePictures)
            {
                if (!newPostAttributePictureIds.Contains(ppap.AttributeId))
                    sqlDatabaseContext.ParsedPostAttributePictures.Remove(ppap);
            }
            await sqlDatabaseContext.SaveChangesAsync(cancellationToken);
        }

        if (parsedPost.AttributeEmbeds.Any())
        {
            foreach (var ppae in parsedPost.AttributeEmbeds)
            {
                if (!newPostAttributeEmbeddedIds.Contains(ppae.UniqueId))
                    sqlDatabaseContext.ParsedPostAttributeEmbeds.Remove(ppae);
            }
            await sqlDatabaseContext.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    protected async Task CreateAsync(Guid apiId, Post post, CancellationToken cancellationToken)
    {
        await using var transaction = await sqlDatabaseContext.Database.BeginTransactionAsync(cancellationToken);

        var parsedPost = new ParsedPost(apiId, post);
        await sqlDatabaseContext.ParsedPosts.AddAsync(parsedPost, cancellationToken);
        await sqlDatabaseContext.SaveChangesAsync(cancellationToken);

        foreach (var postAttribute in post.Attributes)
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

public interface IPostParser
{
    Task ParseAsync(ApiPostMessage message, CancellationToken cancellationToken);
    Task ParseAsync(Guid apiId, Post posts, CancellationToken cancellationToken);
}