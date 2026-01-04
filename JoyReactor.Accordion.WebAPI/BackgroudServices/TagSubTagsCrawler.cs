using JoyReactor.Accordion.Logic.ApiClient;
using JoyReactor.Accordion.Logic.ApiClient.Models;
using JoyReactor.Accordion.Logic.Database.Sql;
using JoyReactor.Accordion.Logic.Database.Sql.Entities;
using JoyReactor.Accordion.Logic.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JoyReactor.Accordion.WebAPI.BackgroudServices;

public class TagSubTagsCrawler(
    SqlDatabaseContext sqlDatabaseContext,
    ITagClient tagClient,
    IOptions<CrawlerSettings> settings,
    ILogger<TagSubTagsCrawler> logger)
    : ScopedBackgroudService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var periodicTimer = new PeriodicTimer(settings.Value.SubsequentRunDelay);
        var tagsWithEmptySubTags = (ParsedTag[])null;

        do
        {
            tagsWithEmptySubTags = await sqlDatabaseContext.ParsedTags
                .Where(tag => tag.MainTagId == null && tag.SubTagsCount > 0 && tag.SubTags.Count() < tag.SubTagsCount)
                .OrderByDescending(tag => tag.Id)
                .Take(100)
                .ToArrayAsync(cancellationToken);

            if (tagsWithEmptySubTags.Length != 0)
                logger.LogInformation("Crawling {TagsCount} tags for new sub tags", tagsWithEmptySubTags.Count());
            else
            {
                logger.LogInformation("No tags without sub tags found. Will try again later");
                continue;
            }

            foreach (var parsedTag in tagsWithEmptySubTags)
            {
                logger.LogInformation("Crawling \"{TagName}\" tag for new sub tags", parsedTag.Name);
                await CrawlAsync(parsedTag, cancellationToken);
            }
        } while (tagsWithEmptySubTags.Length != 0 || await periodicTimer.WaitForNextTickAsync(cancellationToken));
    }

    internal async Task CrawlAsync(ParsedTag parentTag, CancellationToken cancellationToken)
    {
        var subTags = await tagClient.GetAllSubTagsAsync(parentTag.NumberId, TagLineType.NEW, cancellationToken);
        var parsedSubTags = subTags
            .Select(subTag => new ParsedTag(subTag, parentTag))
            .ToArray();
        logger.LogInformation("Found {TagsCount} sub tags in \"{TagName}\" tag", parsedSubTags.Count(), parentTag.Name);

        if (parsedSubTags.Length == 0)
            return;

        await sqlDatabaseContext.ParsedTags.AddRangeIgnoreExistingAsync(parsedSubTags, cancellationToken);
        await sqlDatabaseContext.SaveChangesAsync(cancellationToken);
    }
}