using JoyReactor.Accordion.Logic.Database.Sql;
using JoyReactor.Accordion.Logic.MQ.Messages;
using JoyReactor.Accordion.WebAPI.Models;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JoyReactor.Accordion.WebAPI.BackgroudServices.CatchUps;

public class VectorCreatedCatchUp(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<BackgroundServiceSettings> settings,
    ILogger<VectorCreatedCatchUp> logger)
    : RobustBackgroundService(settings, logger)
{
    protected override bool IsIndefinite => true;
    protected override TimeSpan SubsequentRunDelay => TimeSpan.FromMinutes(15);

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        await using var serviceScope = serviceScopeFactory.CreateAsyncScope();
        await using var sqlDatabaseContext = serviceScope.ServiceProvider.GetRequiredService<SqlDatabaseContext>();
        var publishEndpoint = serviceScope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var attributeIds = await sqlDatabaseContext.ParsedPostAttributePictures
            .Where(picture => picture.IsVectorCreated == true && picture.IsVectorCheckedForDuplicates == false)
            .OrderBy(picture => picture.AttributeId)
            .Select(picture => picture.AttributeId)
            .ToArrayAsync(cancellationToken);

        logger.LogInformation("Catching up {PostAttributes} post attribute picture(s) to check vectors for duplicates.", attributeIds.Length);

        var messages = attributeIds
            .Select(id => new VectorCreatedMessage() { AttributeId = id })
            .ToArray();

        await publishEndpoint.PublishBatch(messages, cancellationToken);
    }
}