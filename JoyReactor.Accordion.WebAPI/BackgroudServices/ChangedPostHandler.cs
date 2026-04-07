using JoyReactor.Accordion.Logic.ApiClient;
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
    protected bool IsFirstRun = true;

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

        var currentDay = DateOnly.FromDateTime(DateTime.UtcNow);

        var startDay = IsFirstRun
            ? currentDay.AddDays(-29)
            : currentDay.AddDays(-1);

        var endDay = currentDay.AddDays(1);

        do
        {
            foreach (var api in apis)
            {
                var changedPosts = await changedPostClient.GetAsync(api, startDay, cancellationToken);
                logger.LogInformation("Found {PostCount} changed post(s) at {Day}.", changedPosts.Length, startDay);

                await postParser.ParseAsync(api, changedPosts, cancellationToken);
            }

            startDay = startDay.AddDays(1);
        } while (startDay <= endDay);

        if (IsFirstRun)
            IsFirstRun = false;
    }
}