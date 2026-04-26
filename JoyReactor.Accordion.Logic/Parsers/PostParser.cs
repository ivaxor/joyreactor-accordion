using JoyReactor.Accordion.Logic.ApiClient.Models;
using JoyReactor.Accordion.Logic.Database.Sql;
using JoyReactor.Accordion.Logic.Database.Sql.Entities;
using JoyReactor.Accordion.Logic.MQ.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JoyReactor.Accordion.Logic.Parsers;

public class PostParser(
    SqlDatabaseContext sqlDatabaseContext,
    ILogger<PostParser> logger)
    : IPostParser
{
    public Task<PostParserResult?> ParseAsync(ApiPostCreatedMessage message, CancellationToken cancellationToken)
    {
        return ParseAsync(message.ApiId, message.Post, false, cancellationToken);
    }

    public Task<PostParserResult?> ParseAsync(Guid apiId, Post post, CancellationToken cancellationToken)
    {
        return ParseAsync(apiId, post, false, cancellationToken);
    }

    public async Task<PostParserResult?> ParseAsync(
        Guid apiId,
        Post post,
        bool ignoreContentVersion,
        CancellationToken cancellationToken)
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
            return await UpdateAsync(parsedPost, post, ignoreContentVersion, cancellationToken);
        else
            return await CreateAsync(apiId, post, cancellationToken);
    }

    protected async Task<PostParserResult?> UpdateAsync(
        ParsedPost parsedPost,
        Post post,
        bool ignoreContentVersion,
        CancellationToken cancellationToken)
    {
        if (ignoreContentVersion == false && post.ContentVersion <= parsedPost.ContentVersion)
            return null;

        await using var transaction = await sqlDatabaseContext.Database.BeginTransactionAsync(cancellationToken);

        parsedPost.ContentVersion = post.ContentVersion.Value;
        parsedPost.UpdatedAt = DateTime.UtcNow;

        var brokenPostAttibuteEmbeds = parsedPost.AttributeEmbeds
            .Where(ppae => ppae.BandCampId == null && ppae.CoubId == null && ppae.SoundCloudId == null && ppae.VimeoId == null && ppae.YouTubeId == null)
            .ToArray();
        if (brokenPostAttibuteEmbeds.Length != 0)
        {
            logger.LogWarning("Found {PostAttributes} broken embedded attribute(s) in {PostNumberId} post.", brokenPostAttibuteEmbeds.Length, parsedPost.NumberId);

            sqlDatabaseContext.ParsedPostAttributeEmbeds.RemoveRange(brokenPostAttibuteEmbeds);
            await sqlDatabaseContext.SaveChangesAsync();
        }

        var existingPostAttributePictureIds = parsedPost.AttributePictures
            .Select(ppap => ppap.AttributeId)
            .ToHashSet();
        var existingPostAttributeEmbeddedIds = parsedPost.AttributeEmbeds
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
                    if (existingPostAttributePictureIds.Contains(postAttribute.NumberId))
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
                    if (existingPostAttributeEmbeddedIds.Contains(parsedAttributeEmbedded.UniqueId))
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

        return new PostParserResult()
        {
            Post = parsedPost,
            PostAttributePictureNumberIds = newPostAttributePictureIds.ToArray(),
            PostAttributeEmbeddedUniqueIds = newPostAttributeEmbeddedIds.ToArray(),
            NewPostAttributePictureNumberIds = newPostAttributePictureIds.Except(existingPostAttributePictureIds).ToArray(),
            NewPostAttributeEmbeddedUniqueIds = newPostAttributeEmbeddedIds.Except(existingPostAttributeEmbeddedIds).ToArray(),
        };
    }

    protected async Task<PostParserResult> CreateAsync(Guid apiId, Post post, CancellationToken cancellationToken)
    {
        await using var transaction = await sqlDatabaseContext.Database.BeginTransactionAsync(cancellationToken);

        var parsedPost = new ParsedPost(apiId, post);
        await sqlDatabaseContext.ParsedPosts.AddAsync(parsedPost, cancellationToken);
        await sqlDatabaseContext.SaveChangesAsync(cancellationToken);

        var newPostAttributePictureIds = new HashSet<int>();
        var newPostAttributeEmbeddedIds = new HashSet<string>();

        foreach (var postAttribute in post.Attributes)
        {
            switch (postAttribute.Type)
            {
                case "PICTURE":
                    newPostAttributePictureIds.Add(postAttribute.NumberId);

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

        return new PostParserResult()
        {
            Post = parsedPost,
            PostAttributePictureNumberIds = newPostAttributePictureIds.ToArray(),
            PostAttributeEmbeddedUniqueIds = newPostAttributeEmbeddedIds.ToArray(),
            NewPostAttributePictureNumberIds = newPostAttributePictureIds.ToArray(),
            NewPostAttributeEmbeddedUniqueIds = newPostAttributeEmbeddedIds.ToArray(),
        };
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
    Task<PostParserResult?> ParseAsync(ApiPostCreatedMessage message, CancellationToken cancellationToken);
    Task<PostParserResult?> ParseAsync(Guid apiId, Post post, CancellationToken cancellationToken);
    Task<PostParserResult?> ParseAsync(Guid apiId, Post post, bool ignoreContentVersion, CancellationToken cancellationToken);
}