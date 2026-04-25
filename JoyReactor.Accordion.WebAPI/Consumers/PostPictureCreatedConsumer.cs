using JoyReactor.Accordion.Logic.Database.Sql;
using JoyReactor.Accordion.Logic.Database.Sql.Entities;
using JoyReactor.Accordion.Logic.Database.Vector;
using JoyReactor.Accordion.Logic.Database.Vector.Extensions;
using JoyReactor.Accordion.Logic.Media;
using JoyReactor.Accordion.Logic.MQ.Messages;
using JoyReactor.Accordion.Logic.Onnx;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Net;

namespace JoyReactor.Accordion.WebAPI.Consumers;

public class PostPictureCreatedConsumer(
    SqlDatabaseContext sqlDatabaseContext,
    IMediaDownloader mediaDownloader,
    IOnnxVectorConverter onnxVectorConverter,
    IQdrantClient qdrantClient,
    IPublishEndpoint publishEndpoint,
    IOptions<QdrantSettings> qdrantSettings,
    IOptions<MediaSettings> mediaSettings,
    ILogger<PostPictureCreatedConsumer> logger)
    : IConsumer<PostPictureCreatedMessage>
{
    public static readonly ParsedPostAttributePictureType[] SupportedImageTypes = [
        ParsedPostAttributePictureType.PNG,
        ParsedPostAttributePictureType.JPEG,
        ParsedPostAttributePictureType.GIF,
        ParsedPostAttributePictureType.BMP,
        ParsedPostAttributePictureType.TIFF,
        ParsedPostAttributePictureType.MP4,
        ParsedPostAttributePictureType.WEBM,
        ParsedPostAttributePictureType.WEBP,
    ];

    public async Task Consume(ConsumeContext<PostPictureCreatedMessage> context)
    {
        var picture = await sqlDatabaseContext.ParsedPostAttributePictures
            .Include(ppap => ppap.Post)
            .ThenInclude(pp => pp.Api)
            .Where(ppap => ppap.AttributeId == context.Message.AttributeId)
            .FirstAsync(context.CancellationToken);

        if (picture.IsVectorCreated == true)
            return;

        if (!SupportedImageTypes.Contains(picture.ImageType))
            return;

        using var image = await DownloadReducedAsync(picture, context.CancellationToken);
        if (image == null)
        {
            await sqlDatabaseContext.SaveChangesAsync(context.CancellationToken);
            return;
        }

        var vector = await onnxVectorConverter.ConvertAsync(image);
        picture.IsVectorCreated = true;
        picture.UpdatedAt = DateTime.UtcNow;

        await qdrantClient.UpsertAsync(qdrantSettings.Value.CollectionName, picture, vector, context.CancellationToken);
        await sqlDatabaseContext.SaveChangesAsync(context.CancellationToken);

        var message = new VectorCreatedMessage() { AttributeId = picture.AttributeId };
        await publishEndpoint.Publish(message, context.CancellationToken);
    }

    protected async Task<Image<Rgb24>?> DownloadReducedAsync(ParsedPostAttributePicture picture, CancellationToken cancellationToken)
    {
        // Jitter
        await Task.Delay(Random.Shared.Next(mediaSettings.Value.SubsequentCallDelay.Milliseconds, mediaSettings.Value.SubsequentCallDelay.Milliseconds * 3), cancellationToken);

        try
        {
            return await mediaDownloader.DownloadReducedAsync(picture, cancellationToken);
        }
        catch (HttpRequestException ex)
        when (
        ex.Message.StartsWith("No such host is known.", StringComparison.Ordinal) ||
        ex.Message.StartsWith("Name or service not known", StringComparison.Ordinal) ||
        ex.Message.StartsWith("The requested name is valid, but no data of the requested type was found.", StringComparison.Ordinal))
        {
            picture.NoContentDueToDns = true;
            picture.UpdatedAt = DateTime.UtcNow;

            logger.LogWarning("Failed to download {PictureAttributeId} post attribute picture due DNS issues. Adding it to temporary ignore list.", picture.AttributeId);
            return null;
        }
        catch (HttpRequestException ex)
        when (ex.StatusCode != null)
        {
            switch (ex.StatusCode)
            {
                case HttpStatusCode.NotFound:
                    // TODO: Try to download mp4/webm if download failed and type is GIF
                    picture.NoContent = true;
                    picture.UpdatedAt = DateTime.UtcNow;
                    logger.LogWarning("Failed to download {PictureAttributeId} post attribute picture due to no content.", picture.AttributeId);
                    break;

                case HttpStatusCode.Forbidden:
                    picture.NoContent = true;
                    picture.UpdatedAt = DateTime.UtcNow;
                    logger.LogWarning("Failed to download {PictureAttributeId} post attribute picture due inaccessible content.", picture.AttributeId);
                    break;

                default:
                    throw;
            }

            return null;
        }
        catch (InvalidImageContentException ex)
        {
            picture.UnsupportedContent = true;
            picture.UpdatedAt = DateTime.UtcNow;
            logger.LogWarning("Failed to create image for {PictureAttributeId} post attribute picture due to invalid image content.", picture.AttributeId);
            return null;
        }
        catch (UnknownImageFormatException ex)
        {
            picture.UnsupportedContent = true;
            picture.UpdatedAt = DateTime.UtcNow;
            logger.LogWarning("Failed to create image for {PictureAttributeId} post attribute picture due to unknown image format.", picture.AttributeId);
            return null;
        }
        catch (NotSupportedException ex)
        {
            picture.UnsupportedContent = true;
            picture.UpdatedAt = DateTime.UtcNow;
            logger.LogWarning("Failed to create image for {PictureAttributeId} post attribute picture due to unsupported content.", picture.AttributeId);
            return null;
        }
        catch (ArgumentOutOfRangeException ex)
        when (
        (ex.Source == "SixLabors.ImageSharp" && ex.Message.Contains("DangerousGetRowSpan", StringComparison.Ordinal)) ||
        (ex.Source == "System.Private.CoreLib" && ex.Message.Equals("Specified argument was out of the range of valid values.", StringComparison.Ordinal)))
        {
            picture.UnsupportedContent = true;
            picture.UpdatedAt = DateTime.UtcNow;
            logger.LogWarning("Failed to create image for {PictureAttributeId} post attribute picture due to broken content.", picture.AttributeId);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to download {PictureAttributeId} post attribute picture. Adding it to temporary ignore list.", picture.AttributeId);
            return null;
        }
    }
}

public class PostPictureCreatedConsumerDefinition : ConsumerDefinition<PostPictureCreatedConsumer>
{
    public PostPictureCreatedConsumerDefinition()
    {
        EndpointName = "post_picture_created";
        ConcurrentMessageLimit = 5;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<PostPictureCreatedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retryConfurator => retryConfurator.Interval(3, TimeSpan.FromSeconds(5)));
    }
}