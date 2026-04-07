using JoyReactor.Accordion.Logic.ApiClient;
using JoyReactor.Accordion.Logic.ApiClient.Models;
using JoyReactor.Accordion.Logic.Database.Sql;
using JoyReactor.Accordion.Logic.Parsers;
using JoyReactor.Accordion.WebAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JoyReactor.Accordion.WebAPI.BackgroudServices;

public class ChangedPostHandler(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<BackgroundServiceSettings> settings,
    ILogger<ChangedPostHandler> logger)
    : RobustBackgroundService(settings, logger)
{
    protected override bool IsIndefinite => true;

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        await using var serviceScope = serviceScopeFactory.CreateAsyncScope();
        await using var sqlDatabaseContext = serviceScope.ServiceProvider.GetRequiredService<SqlDatabaseContext>();
        var changedPostClient = serviceScope.ServiceProvider.GetRequiredService<IChangedPostClient>();
        var postParser = serviceScope.ServiceProvider.GetRequiredService<IPostParser>();

        var apis = await sqlDatabaseContext.CrawlerTasks
            .AsNoTracking()
            .Select(task => task.Tag.Api!)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        var previousDay = DateOnly.FromDateTime(DateTime.UtcNow - TimeSpan.FromHours(12));
        var currentDay = DateOnly.FromDateTime(DateTime.UtcNow);
        var nextDay = DateOnly.FromDateTime(DateTime.UtcNow + TimeSpan.FromHours(12));

        var queryPreviousDay = previousDay != currentDay;
        var queryNextDay = nextDay != currentDay;

        foreach (var api in apis)
        {
            var previousDayChangedPosts = queryPreviousDay
                ? await changedPostClient.GetAsync(api, previousDay, cancellationToken)
                : [];

            var currentDayChangedPosts = await changedPostClient.GetAsync(api, currentDay, cancellationToken);

            var nextDayChangedPosts = queryNextDay
                ? await changedPostClient.GetAsync(api, nextDay, cancellationToken)
                : [];

            Post[] changedPosts = [.. previousDayChangedPosts, .. currentDayChangedPosts, .. nextDayChangedPosts];
            logger.LogInformation("Found {PostCount} changed posts.", changedPosts.Length);

            await postParser.ParseAsync(api, changedPosts, cancellationToken);
        }
    }
}