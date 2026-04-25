using JoyReactor.Accordion.Logic.Database.Sql;
using JoyReactor.Accordion.Logic.MQ.Messages;
using JoyReactor.Accordion.WebAPI.Consumers;
using JoyReactor.Accordion.WebAPI.Models;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JoyReactor.Accordion.WebAPI.BackgroudServices.CatchUps;

public class PostPictureCreatedCatchUp(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<BackgroundServiceSettings> settings,
    ILogger<PostPictureCreatedCatchUp> logger)
    : RobustBackgroundService(settings, logger)
{
    protected override bool IsIndefinite => true;
    protected override TimeSpan SubsequentRunDelay => TimeSpan.FromHours(1);

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        await using var serviceScope = serviceScopeFactory.CreateAsyncScope();
        await using var sqlDatabaseContext = serviceScope.ServiceProvider.GetRequiredService<SqlDatabaseContext>();
        var publishEndpoint = serviceScope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var attributeIds = await sqlDatabaseContext.ParsedPostAttributePictures
            .Where(picture => picture.IsVectorCreated == false)
            .Where(picture => picture.NoContent == false && picture.NoContentDueToDns == false && picture.UnsupportedContent == false)
            .Where(picture => PostPictureCreatedConsumer.SupportedImageTypes.Contains(picture.ImageType))
            .OrderBy(picture => picture.AttributeId)
            .Select(picture => picture.AttributeId)
            .ToArrayAsync(cancellationToken);

        var messages = attributeIds
            .Select(id => new PostPictureCreatedMessage() { AttributeId = id })
            .ToArray();

        await publishEndpoint.PublishBatch(messages, cancellationToken);
    }
}