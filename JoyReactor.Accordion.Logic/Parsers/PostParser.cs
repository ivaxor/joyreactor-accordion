using JoyReactor.Accordion.Logic.ApiClient.Models;
using JoyReactor.Accordion.Logic.Database.Sql;
using JoyReactor.Accordion.Logic.Database.Sql.Entities;
using JoyReactor.Accordion.Logic.Database.Sql.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JoyReactor.Accordion.Logic.Parsers;

public class PostParser(
    SqlDatabaseContext sqlDatabaseContext,
    ILogger<PostParser> logger)
    : IPostParser
{
    public Task ParseAsync(Post post, CancellationToken cancellationToken)
    {
        return ParseAsync([post], cancellationToken);
    }

    public async Task ParseAsync(IEnumerable<Post> posts, CancellationToken cancellationToken)
    {
        if (posts.Count() == 0)
            return;

        await using var transaction = await sqlDatabaseContext.Database.BeginTransactionAsync(cancellationToken);
        var postNumberIds = posts.Select(p => p.NumberId).ToArray();
        var existingPostContentVersions = await sqlDatabaseContext.ParsedPosts
            .AsNoTracking()
            .Where(post => postNumberIds.Contains(post.NumberId))
            .ToDictionaryAsync(post => post.NumberId, post => post.ContentVersion, cancellationToken);
        foreach (var post in posts)
        {
            if (existingPostContentVersions.TryGetValue(post.NumberId, out var contentVersion) && post.ContentVersion != contentVersion)
            {
                logger.LogInformation("Post {PostNubmerId} content version changed. Deleting old post information.", post.NumberId);
                await sqlDatabaseContext.ParsedPosts.Where(p => p.NumberId == post.NumberId).ExecuteDeleteAsync(cancellationToken);
            }
        }
        await sqlDatabaseContext.SaveChangesAsync(cancellationToken);

        var parsedPosts = new List<ParsedPost>(posts.Count());
        var parsedPostAttributes = new List<IParsedPostAttribute>();
        var parsedAttributeEmbeds = new List<IParsedAttributeEmbedded>();
        foreach (var post in posts)
        {
            if (existingPostContentVersions.TryGetValue(post.NumberId, out var contentVersion) && post.ContentVersion == contentVersion)
            {
                logger.LogDebug("Post {PostNumberId} content version change didn't changed. Skipping post.", post.NumberId);
                continue;
            }

            var parsedPost = new ParsedPost(post);
            parsedPosts.Add(parsedPost);

            foreach (var postAttribute in post.Attributes)
            {
                var parsedAttributeEmbedded = await CreateAttributeAsync(postAttribute, cancellationToken);
                var existingDatabaseParsedAttributeEmbedded = await GetExistingDatabaseAttributeAsync(parsedAttributeEmbedded, cancellationToken);
                var existingLocalParsedAttributeEmbedded = GetExistingLocalAttribute(parsedAttributeEmbeds, parsedAttributeEmbedded);
                if (parsedAttributeEmbedded != null && existingDatabaseParsedAttributeEmbedded == null && existingLocalParsedAttributeEmbedded == null)
                    parsedAttributeEmbeds.Add(parsedAttributeEmbedded);

                var existingParsedAttributeEmbedded = existingLocalParsedAttributeEmbedded
                    ?? existingDatabaseParsedAttributeEmbedded
                    ?? parsedAttributeEmbedded;

                var parsedPostAttribute = CreatePostAttribute(postAttribute, parsedPost, existingParsedAttributeEmbedded);
                parsedPostAttributes.Add(parsedPostAttribute);
            }
        }

        await sqlDatabaseContext.ParsedPosts.AddRangeAsync(parsedPosts, cancellationToken);
        await AddRangeAsync(parsedPostAttributes, cancellationToken);
        await UpsertRangeAsync(parsedAttributeEmbeds, cancellationToken);

        await sqlDatabaseContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    protected async Task UpsertRangeAsync(IEnumerable<IParsedAttributeEmbedded> parsedAttributeEmbeds, CancellationToken cancellationToken)
    {
        foreach (var group in parsedAttributeEmbeds.GroupBy(attribute => attribute.GetType()))
        {
            await (group.First() switch
            {
                ParsedBandCamp => sqlDatabaseContext.ParsedBandCamps.UpsertRangeAsync(group.Cast<ParsedBandCamp>(), cancellationToken),
                ParsedCoub => sqlDatabaseContext.ParsedCoubs.UpsertRangeAsync(group.Cast<ParsedCoub>(), cancellationToken),
                ParsedSoundCloud => sqlDatabaseContext.ParsedSoundClouds.UpsertRangeAsync(group.Cast<ParsedSoundCloud>(), cancellationToken),
                ParsedVimeo => sqlDatabaseContext.ParsedVimeos.UpsertRangeAsync(group.Cast<ParsedVimeo>(), cancellationToken),
                ParsedYouTube => sqlDatabaseContext.ParsedYouTubes.UpsertRangeAsync(group.Cast<ParsedYouTube>(), cancellationToken),
                _ => throw new NotImplementedException(),
            });
        }
    }

    protected async Task AddRangeAsync(IEnumerable<IParsedPostAttribute> parsedPostAttributes, CancellationToken cancellationToken)
    {
        foreach (var group in parsedPostAttributes.GroupBy(postAttribute => postAttribute.GetType()))
        {
            await (group.First() switch
            {
                ParsedPostAttributePicture => sqlDatabaseContext.ParsedPostAttributePictures.AddRangeAsync(group.Cast<ParsedPostAttributePicture>(), cancellationToken),
                ParsedPostAttributeEmbedded => sqlDatabaseContext.ParsedPostAttributeEmbeds.AddRangeAsync(group.Cast<ParsedPostAttributeEmbedded>(), cancellationToken),
                _ => throw new NotImplementedException(),
            });
        }
    }

    protected async Task<IParsedAttributeEmbedded?> CreateAttributeAsync(PostAttribute postAttribute, CancellationToken cancellationToken)
    {
        return postAttribute.Type switch
        {
            "PICTURE" => null,
            "BANDCAMP" => new ParsedBandCamp(postAttribute),
            "COUB" => new ParsedCoub(postAttribute),
            "SOUNDCLOUD" => new ParsedSoundCloud(postAttribute),
            "VIMEO" => new ParsedVimeo(postAttribute),
            "YOUTUBE" => new ParsedYouTube(postAttribute),
            _ => throw new NotImplementedException(),
        };
    }

    protected async Task<IParsedAttributeEmbedded?> GetExistingDatabaseAttributeAsync(IParsedAttributeEmbedded? parsedAttribute, CancellationToken cancellationToken)
    {
        return parsedAttribute switch
        {
            null => null,
            ParsedBandCamp parsedBandCamp => await sqlDatabaseContext.ParsedBandCamps.Where(bandCamp => bandCamp.UrlPath == parsedBandCamp.UrlPath).FirstOrDefaultAsync(cancellationToken),
            ParsedCoub parsedCoub => await sqlDatabaseContext.ParsedCoubs.Where(coub => coub.VideoId == parsedCoub.VideoId).FirstOrDefaultAsync(cancellationToken),
            ParsedSoundCloud parsedSoundCloud => await sqlDatabaseContext.ParsedSoundClouds.Where(soundCloud => soundCloud.UrlPath == parsedSoundCloud.UrlPath).FirstOrDefaultAsync(cancellationToken),
            ParsedVimeo parsedVimeo => await sqlDatabaseContext.ParsedVimeos.Where(vimeo => vimeo.VideoId == parsedVimeo.VideoId).FirstOrDefaultAsync(cancellationToken),
            ParsedYouTube parsedYouTube => await sqlDatabaseContext.ParsedYouTubes.Where(youTube => youTube.VideoId == parsedYouTube.VideoId).FirstOrDefaultAsync(cancellationToken),
            _ => throw new NotImplementedException(),
        };
    }

    protected static IParsedAttributeEmbedded? GetExistingLocalAttribute(IEnumerable<IParsedAttributeEmbedded> parsedAttributes, IParsedAttributeEmbedded? parsedAttribute)
    {
        return parsedAttribute switch
        {
            null => null,
            ParsedBandCamp parsedBandCamp => parsedAttributes.Where(pa => pa is ParsedBandCamp).Cast<ParsedBandCamp>().SingleOrDefault(pa => pa.UrlPath == parsedBandCamp.UrlPath),
            ParsedCoub parsedCoub => parsedAttributes.Where(pa => pa is ParsedCoub).Cast<ParsedCoub>().SingleOrDefault(pa => pa.VideoId == parsedCoub.VideoId),
            ParsedSoundCloud parsedSoundCloud => parsedAttributes.Where(pa => pa is ParsedSoundCloud).Cast<ParsedSoundCloud>().SingleOrDefault(pa => pa.UrlPath == parsedSoundCloud.UrlPath),
            ParsedVimeo parsedVimeo => parsedAttributes.Where(pa => pa is ParsedVimeo).Cast<ParsedVimeo>().SingleOrDefault(pa => pa.VideoId == parsedVimeo.VideoId),
            ParsedYouTube parsedYouTube => parsedAttributes.Where(pa => pa is ParsedYouTube).Cast<ParsedYouTube>().SingleOrDefault(pa => pa.VideoId == parsedYouTube.VideoId),
            _ => throw new NotImplementedException(),
        };
    }

    protected static IParsedPostAttribute CreatePostAttribute(PostAttribute postAttribute, ParsedPost post, IParsedAttributeEmbedded parsedAttribute)
    {
        return postAttribute.Type switch
        {
            "PICTURE" => new ParsedPostAttributePicture(postAttribute, post),
            "BANDCAMP" or "COUB" or "SOUNDCLOUD" or "VIMEO" or "YOUTUBE" => new ParsedPostAttributeEmbedded(postAttribute, post, parsedAttribute),
            _ => throw new NotImplementedException(),
        };
    }
}

public interface IPostParser
{
    Task ParseAsync(Post post, CancellationToken cancellationToken);
    Task ParseAsync(IEnumerable<Post> posts, CancellationToken cancellationToken);
}