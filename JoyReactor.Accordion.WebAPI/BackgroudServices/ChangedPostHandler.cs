using JoyReactor.Accordion.Logic.ApiClient;
using JoyReactor.Accordion.Logic.Database.Sql;
using JoyReactor.Accordion.Logic.Database.Sql.Entities;
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
    protected override TimeSpan SubsequentRunDelay => settings.Value.SubsequentRunDelay * 3;
    protected override bool IsIndefinite => true;
    protected bool IsFirstRun = true;

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        await using var serviceScope = serviceScopeFactory.CreateAsyncScope();
        await using var sqlDatabaseContext = serviceScope.ServiceProvider.GetRequiredService<SqlDatabaseContext>();

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
                await ParseAsync(api, startDay, cancellationToken);
            }

            startDay = startDay.AddDays(1);
        } while (startDay <= endDay);

        if (IsFirstRun)
            IsFirstRun = false;
    }

    protected async Task ParseAsync(Api api, DateOnly day, CancellationToken cancellationToken)
    {
        await using var serviceScope = serviceScopeFactory.CreateAsyncScope();
        var changedPostClient = serviceScope.ServiceProvider.GetRequiredService<IChangedPostClient>();
        var postParser = serviceScope.ServiceProvider.GetRequiredService<IPostParser>();

        var changedPosts = await changedPostClient.GetAsync(api, day, cancellationToken);
        logger.LogInformation("Found {PostCount} changed post(s) at {Day} for {Api}.", changedPosts.Length, day, api.HostName);

        await postParser.ParseAsync(api, changedPosts, cancellationToken);
    }
}