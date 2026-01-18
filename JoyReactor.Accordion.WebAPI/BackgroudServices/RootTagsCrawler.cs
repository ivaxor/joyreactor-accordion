using JoyReactor.Accordion.Logic.Crawlers;
using JoyReactor.Accordion.Logic.Database.Sql;
using JoyReactor.Accordion.Logic.Database.Sql.Entities;
using JoyReactor.Accordion.WebAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JoyReactor.Accordion.WebAPI.BackgroudServices;

public class RootTagsCrawler(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<BackgroundServiceSettings> settings,
    ILogger<RootTagsCrawler> logger)
    : RobustBackgroundService(settings, logger)
{
    protected override bool IsIndefinite => false;

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        await using var serviceScope = serviceScopeFactory.CreateAsyncScope();
        await using var sqlDatabaseContext = serviceScope.ServiceProvider.GetRequiredService<SqlDatabaseContext>();
        var tagCrawler = serviceScope.ServiceProvider.GetRequiredService<ITagCrawler>();

        var apis = await sqlDatabaseContext.Apis
            .AsNoTracking()
            .ToArrayAsync(cancellationToken);

        foreach (var api in apis)
        {
            try
            {
                logger.LogInformation("Crawling root tags for {Api}.", api.HostName);
                await CrawlAsync(sqlDatabaseContext, tagCrawler, api, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to crawl root tags for {Api}", api.HostName);
            }
        }
    }

    protected async Task CrawlAsync(
        SqlDatabaseContext sqlDatabaseContext,
        ITagCrawler tagCrawler,
        Api api,
        CancellationToken cancellationToken)
    {
        var existingRootTagNames = await sqlDatabaseContext.ParsedTags
            .AsNoTracking()
            .Include(tag => tag.Api)
            .Where(tagName => api.RootTagNames.Contains(tagName.Name))
            .Select(tag => tag.Name)
            .ToHashSetAsync(StringComparer.Ordinal, cancellationToken);

        var nonExistingRootTagNames = api.RootTagNames
            .Where(tagName => !existingRootTagNames.Contains(tagName))
            .ToArray();

        if (nonExistingRootTagNames.Length == 0)
        {
            logger.LogInformation("No new root tags found for {Api}.", api.HostName);
            return;
        }
        logger.LogInformation("Crawling {TagsCount} root tag(s) for {Api}.", nonExistingRootTagNames.Count(), api.HostName);

        foreach (var tagName in nonExistingRootTagNames)
            await tagCrawler.CrawlAsync(api, tagName, cancellationToken);
    }
}