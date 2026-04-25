using JoyReactor.Accordion.Logic.ApiClient;
using JoyReactor.Accordion.Logic.ApiClient.Models;
using JoyReactor.Accordion.Logic.Database.Sql;
using JoyReactor.Accordion.Logic.Extensions;
using JoyReactor.Accordion.Logic.MQ.Messages;
using JoyReactor.Accordion.WebAPI.Models;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace JoyReactor.Accordion.WebAPI.BackgroudServices.Publishers;

public class CrawlerApiPostPublisher(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<BackgroundServiceSettings> settings,
    ILogger<CrawlerApiPostPublisher> logger)
    : RobustBackgroundService(settings, logger)
{
    protected override bool IsIndefinite => true;
    protected override TimeSpan SubsequentRunDelay => TimeSpan.FromMinutes(1);

    protected readonly ConcurrentDictionary<Guid, (Task Task, CancellationTokenSource Cts)> TaskWithCancellationTokenSources = new ConcurrentDictionary<Guid, (Task Task, CancellationTokenSource Cts)>();

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        await using var serviceScope = serviceScopeFactory.CreateAsyncScope();
        await using var sqlDatabaseContext = serviceScope.ServiceProvider.GetRequiredService<SqlDatabaseContext>();

        var crawlerTasks = await sqlDatabaseContext.CrawlerTasks
            .AsNoTracking()
            .ToDictionaryAsync(crawlerTask => crawlerTask.Id, crawlerTask => crawlerTask, cancellationToken);

        var completedTasks = TaskWithCancellationTokenSources
            .Where(task => task.Value.Item1.IsCompleted)
            .ToArray();
        foreach (var (id, (task, cts)) in completedTasks)
        {
            if (task.IsFaulted)
                logger.LogError(task.Exception, "Crawler task {CrawlerTaskId} failed. Message: {ExceptionMessage}.", id, task.Exception.Message);

            if (TaskWithCancellationTokenSources.TryRemove(id, out var _))
                cts.Dispose();
        }

        var newCrawlerTasks = crawlerTasks
            .Where(crawkerTask => !TaskWithCancellationTokenSources.ContainsKey(crawkerTask.Key))
            .ToArray();
        foreach (var (id, crawlerTask) in newCrawlerTasks)
        {
            logger.LogInformation("Starting {CrawlerTaskId} crawler task.", id);
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var ct = cts.Token;
            var task = Task.Run(() => CrawlAsync(crawlerTask.Id, ct));

            TaskWithCancellationTokenSources.TryAdd(id, new(task, cts));
        }

        var oldTasks = TaskWithCancellationTokenSources
            .Where(kvp => !kvp.Value.Cts.IsCancellationRequested && !crawlerTasks.ContainsKey(kvp.Key))
            .ToArray();
        foreach (var (id, (task, cts)) in oldTasks)
        {
            logger.LogInformation("Stopping {CrawlerTaskId} crawler task.", id);
            await cts.CancelAsync();
        }
    }

    protected async Task CrawlAsync(Guid crawlerTaskId, CancellationToken cancellationToken)
    {
        using (logger.BeginScope(new Dictionary<string, object>() { { "CrawlerTaskId", crawlerTaskId } }))
        {
            await using var serviceScope = serviceScopeFactory.CreateAsyncScope();
            await using var sqlDatabaseContext = serviceScope.ServiceProvider.GetRequiredService<SqlDatabaseContext>();
            var postClient = serviceScope.ServiceProvider.GetRequiredService<IPostClient>();
            var publishEndpoint = serviceScope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

            var crawlerTask = await sqlDatabaseContext.CrawlerTasks
                .Include(task => task.Tag)
                .ThenInclude(tag => tag.Api)
                .Where(task => task.Id == crawlerTaskId)
                .FirstAsync(cancellationToken);
            crawlerTask.StartedAt = DateTime.UtcNow;
            crawlerTask.UpdatedAt = DateTime.UtcNow;
            sqlDatabaseContext.CrawlerTasks.Update(crawlerTask);
            await sqlDatabaseContext.SaveChangesAsync(cancellationToken);

            var postPager = (PostPager)null;
            var isLastPage = false;
            do
            {
                cancellationToken.ThrowIfCancellationRequested();

                var tagNumberId = crawlerTask.TagId.ToInt();
                postPager = await postClient.GetByTagAsync(crawlerTask.Tag.Api, tagNumberId, crawlerTask.PostLineType, crawlerTask.PageCurrent, cancellationToken);
                isLastPage = crawlerTask.PageCurrent >= postPager.LastPage;

                var messages = postPager.Posts
                    .Select(p => new ApiPostCreatedMessage() { ApiId = crawlerTask.Tag.Api.Id, Post = p })
                    .ToArray();
                await publishEndpoint.PublishBatch(messages, cancellationToken);

                logger.LogInformation("Found {PostCount} post(s) using \"{TagName}\" tag in {HostName} on {Page}/{PageLast} page.", messages.Length, crawlerTask.Tag.Name, crawlerTask.Tag.Api.HostName, crawlerTask.PageCurrent, postPager.LastPage);

                if (isLastPage)
                    crawlerTask.FinishedAt = DateTime.UtcNow;
                else
                    crawlerTask.PageCurrent += 1;
                crawlerTask.PageLast = postPager.LastPage;
                crawlerTask.UpdatedAt = DateTime.UtcNow;
                sqlDatabaseContext.CrawlerTasks.Update(crawlerTask);
                await sqlDatabaseContext.SaveChangesAsync(cancellationToken);
            } while (!isLastPage);

            logger.LogInformation("Crawler task {CrawlerTaskId} finished.", crawlerTask.Id);
        }
    }
}