using JoyReactor.Accordion.Database;
using JoyReactor.Accordion.Logic.ApiClient;
using JoyReactor.Accordion.Logic.ApiClient.Constants;
using JoyReactor.Accordion.Logic.ApiClient.Models;
using JoyReactor.Accordion.Logic.Database.Sql.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace JoyReactor.Accordion.Workers.HostedServices;

public class TagCrawlerWorker(
    ITagClient tagClient,
    SqlDatabaseContext sqlDatabaseContext)
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
            await Crawl(tagName, cancellationToken);

        var tagsWithEmptySubTags = await sqlDatabaseContext.ParsedTags
                .Where(tag => tag.SubTagsCount > 0 && tag.SubTags.Count() < tag.SubTagsCount)
                .ToArrayAsync(cancellationToken);
        while (tagsWithEmptySubTags.Length != 0)
        {
            foreach (var parsedTag in tagsWithEmptySubTags)
                await Crawl(parsedTag, cancellationToken);

            tagsWithEmptySubTags = await sqlDatabaseContext.ParsedTags
                .Where(tag => tag.SubTagsCount > 0 && tag.SubTags.Count() < tag.SubTagsCount)
                .ToArrayAsync(cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
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