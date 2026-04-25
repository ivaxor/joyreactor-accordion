using JoyReactor.Accordion.Logic.ApiClient;
using JoyReactor.Accordion.Logic.Database.Sql;
using JoyReactor.Accordion.Logic.MQ.Messages;
using JoyReactor.Accordion.WebAPI.Models;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JoyReactor.Accordion.WebAPI.BackgroudServices.Publishers;

public class ChangedApiPostPublisher(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<BackgroundServiceSettings> settings,
    ILogger<ChangedApiPostPublisher> logger)
    : RobustBackgroundService(settings, logger)
{
    protected override TimeSpan SubsequentRunDelay => TimeSpan.FromMinutes(3);
    protected override bool IsIndefinite => true;
    protected bool IsFirstRun = true;

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        await using var serviceScope = serviceScopeFactory.CreateAsyncScope();
        await using var sqlDatabaseContext = serviceScope.ServiceProvider.GetRequiredService<SqlDatabaseContext>();
        var changedPostClient = serviceScope.ServiceProvider.GetRequiredService<IChangedPostClient>();
        var publishEndpoint = serviceScope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

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

                var messages = changedPosts
                    .Select(p => new ApiPostCreatedMessage() { ApiId = api.Id, Post = p })
                    .ToArray();
                await publishEndpoint.PublishBatch(messages, cancellationToken);

                logger.LogInformation("Found {PostCount} changed post(s) at {Day} for {Api}.", changedPosts.Length, startDay, api.HostName);
            }

            startDay = startDay.AddDays(1);
        } while (startDay <= endDay);

        if (IsFirstRun)
            IsFirstRun = false;
    }
}