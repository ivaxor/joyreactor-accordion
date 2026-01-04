using JoyReactor.Accordion.Logic.ApiClient;
using JoyReactor.Accordion.Logic.ApiClient.Models;
using JoyReactor.Accordion.Logic.Database.Sql;
using JoyReactor.Accordion.Logic.Database.Sql.Entities;
using JoyReactor.Accordion.Logic.Extensions;
using Microsoft.EntityFrameworkCore;

namespace JoyReactor.Accordion.WebAPI.BackgroudServices;

public class TagInnnerRangeCrawler(
    SqlDatabaseContext sqlDatabaseContext,
    ITagClient tagClient,
    ILogger<TagInnnerRangeCrawler> logger)
    : ScopedBackgroudService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var take = 100;
        var skip = 0;
        var tagNumbers = (HashSet<int>)null;

        var tagStartNumber = 0;
        var tagEndNumber = 0;

        do
        {
            tagNumbers = await sqlDatabaseContext.ParsedTags
                .OrderBy(tag => tag.NumberId)
                .Select(tag => tag.NumberId)
                .Take(take)
                .Skip(skip)
                .ToHashSetAsync(cancellationToken);
            if (tagNumbers.Count == 0)
            {
                logger.LogInformation("No tags found. Will try again later");
                continue;
            }

            tagStartNumber = tagNumbers.First();
            tagEndNumber = tagNumbers.Last();

            var emptyTags = await sqlDatabaseContext.EmptyTags
                .Where(tag => tag.NumberId >= tagStartNumber && tag.NumberId <= tagEndNumber)
                .Select(tag => tag.NumberId)
                .ToHashSetAsync(cancellationToken);

            for (var tagNumber = tagStartNumber; tagNumber <= tagEndNumber; tagNumber++)
            {
                if (tagNumbers.Contains(tagNumber) || emptyTags.Contains(tagNumber))
                    continue;

                var tag = await tagClient.GetAsync(tagNumber, TagLineType.NEW, cancellationToken);
                if (tag == null)
                {
                    logger.LogInformation("No tag found with {TagNumberId} number id", tagNumber);

                    var emptyTag = new EmptyTag(tagNumber);
                    await sqlDatabaseContext.EmptyTags.AddIgnoreExistingAsync(emptyTag, cancellationToken);
                }
                else
                {
                    logger.LogInformation("Found \"{TagName}\" tag with {TagNumberId} number id", tag.Name, tag.NumberId);

                    await CrawlMainTagAsync(tag, cancellationToken);
                    await CrawlParentTagAsync(tag, cancellationToken);

                    var parsedTag = new ParsedTag(tag);
                    await sqlDatabaseContext.ParsedTags.AddIgnoreExistingAsync(parsedTag, cancellationToken);


                    skip++;
                }

                await sqlDatabaseContext.SaveChangesAsync(cancellationToken);
            }

            skip += take;
        } while (tagNumbers.Count > 0);
    }

    internal async Task CrawlMainTagAsync(Tag tag, CancellationToken cancellationToken)
    {
        var mainTag = tag.MainTag;
        if (mainTag == null)
            return;

        logger.LogInformation("Crawling main tag for \"{TagName}\" tag", tag.Name);
        await Crawl(mainTag.NumberId, cancellationToken);
    }

    internal async Task CrawlParentTagAsync(Tag tag, CancellationToken cancellationToken)
    {
        var parentTag = tag.Hierarchy
            .Where(t => t.NumberId != tag.NumberId)
            .FirstOrDefault();
        if (parentTag == null)
            return;

        logger.LogInformation("Crawling parent tag for \"{TagName}\" tag", tag.Name);
        await Crawl(parentTag.NumberId, cancellationToken);
    }

    internal async Task Crawl(int numberId, CancellationToken cancellationToken)
    {
        var isTagExist = await sqlDatabaseContext.ParsedTags.AnyAsync(tag => tag.NumberId == numberId, cancellationToken);
        if (isTagExist)
            return;

        var tag = await tagClient.GetAsync(numberId, TagLineType.NEW, cancellationToken);
        logger.LogInformation("Found \"{TagName}\" tag with {TagNumberId} number id", tag.Name, tag.NumberId);

        await CrawlMainTagAsync(tag, cancellationToken);
        await CrawlParentTagAsync(tag, cancellationToken);

        var parsedMainTag = new ParsedTag(tag);
        await sqlDatabaseContext.ParsedTags.AddIgnoreExistingAsync(parsedMainTag, cancellationToken);
    }
}