using JoyReactor.Accordion.Logic.ApiClient;
using JoyReactor.Accordion.Logic.ApiClient.Constants;
using JoyReactor.Accordion.Logic.Database.Sql;
using JoyReactor.Accordion.Logic.Database.Sql.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JoyReactor.Accordion.Workers.HostedServices;

public class TagCrawlerWorker(
    ITagClient tagClient,
    SqlDatabaseContext sqlDatabaseContext,
    IOptions<ApiClientSettings> settings,
    ILogger<TagCrawlerWorker> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var existingMainTagNames = await sqlDatabaseContext.ParsedTags
            .Where(tagName => TagConstants.MainCategories.ToArray().Contains(tagName.Name))
            .Select(tag => tag.Name)
            .ToHashSetAsync(StringComparer.Ordinal, cancellationToken);
        var nonExistingMainTagNames = TagConstants.MainCategories
            .Where(tagName => !existingMainTagNames.Contains(tagName))
            .ToArray();
        foreach (var tagName in nonExistingMainTagNames)
        {
            logger.LogInformation("Crawling {TagName} main category tag", tagName);

            await Crawl(tagName, cancellationToken);
            await Task.Delay(settings.Value.SubsequentCallDelay);
        }

        var tagsWithEmptySubTags = await sqlDatabaseContext.ParsedTags
                .Where(tag => tag.SubTagsCount > 0 && tag.SubTags.Count() < tag.SubTagsCount)
                .ToArrayAsync(cancellationToken);
        while (tagsWithEmptySubTags.Length != 0)
        {
            foreach (var parsedTag in tagsWithEmptySubTags)
            {
                logger.LogInformation("Crawling {TagName} tag for sub tags", parsedTag.Name);

                await Crawl(parsedTag, cancellationToken);
                await Task.Delay(settings.Value.SubsequentCallDelay);
            }

            tagsWithEmptySubTags = await sqlDatabaseContext.ParsedTags
                .Where(tag => tag.SubTagsCount > 0 && tag.SubTags.Count() < tag.SubTagsCount)
                .ToArrayAsync(cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    internal async Task Crawl(string tagName, CancellationToken cancellationToken)
    {
        var tag = await tagClient.GetByNameAsync(tagName, cancellationToken);
        var parsedTag = new ParsedTag(tag);

        await sqlDatabaseContext.ParsedTags.AddAsync(parsedTag, cancellationToken);
        await sqlDatabaseContext.SaveChangesAsync(cancellationToken);
    }

    internal async Task Crawl(ParsedTag parsedTag, CancellationToken cancellationToken)
    {
        var subTags = await tagClient.GetSubTagsAsync(parsedTag.NumberId, cancellationToken);
        var parsedTags = subTags
            .Select(tag => new ParsedTag(tag))
            .ToArray();
        logger.LogInformation("Parsed {SubTagsCount} sub tags in {TagName} tag", parsedTags.Count(), parsedTag.Name);

        var parsedTagIds = parsedTags
            .Select(tag => tag.Id)
            .ToArray();
        var existingTagIds = await sqlDatabaseContext.ParsedTags
            .Where(tag => parsedTagIds.Contains(tag.Id))
            .Select(tag => tag.Id)
            .ToHashSetAsync(cancellationToken);

        parsedTags = parsedTags
            .Where(tag => !existingTagIds.Contains(tag.Id))
            .ToArray();

        await sqlDatabaseContext.ParsedTags.AddRangeAsync(parsedTags, cancellationToken);
        await sqlDatabaseContext.SaveChangesAsync(cancellationToken);
    }
}