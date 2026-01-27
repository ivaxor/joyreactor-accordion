using JoyReactor.Accordion.Logic.ApiClient.Models;
using JoyReactor.Accordion.Logic.Database.Sql.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Polly.Timeout;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections.Frozen;
using System.Net;
using System.Net.Mime;

namespace JoyReactor.Accordion.Logic.Media;

public class MediaDownloader(
    HttpClient httpClient,
    IMediaReducer mediaReducer,
    IOptions<MediaSettings> settings,
    ILogger<MediaDownloader> logger)
    : IMediaDownloader
{
    private static readonly ResiliencePropertyKey<string> UrlKey = new("RequestUrl");

    protected readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);

    protected readonly ResiliencePipeline ResiliencePipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            ShouldHandle = new PredicateBuilder()
                .Handle<TimeoutRejectedException>()
                .Handle<HttpRequestException>(),
            MaxRetryAttempts = settings.Value.MaxRetryAttempts,
            Delay = settings.Value.RetryDelay,
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            OnRetry = async args =>
            {
                args.Context.Properties.TryGetValue(UrlKey, out var url);

                switch (args.Outcome.Exception)
                {
                    case HttpRequestException ex:
                        logger.LogWarning("Failed to download media from {Url}. Status code: {StatusCode}. Attempt: {Attempt}/{MaxAttempts}. ", url, ex.StatusCode, args.AttemptNumber + 1, settings.Value.MaxRetryAttempts + 1);
                        break;

                    default:
                        logger.LogWarning("Failed to download media from {Url}. Message: {Message}.  Attempt: {Attempt}/{MaxAttempts}.", url, args.Outcome.Exception.Message, args.AttemptNumber + 1, settings.Value.MaxRetryAttempts + 1);
                        break;
                }
            },
        })
        .AddTimeout(TimeSpan.FromSeconds(100))
        .Build();

    protected static readonly FrozenSet<ParsedPostAttributePictureType> PictureTypes = new HashSet<ParsedPostAttributePictureType>() {
        ParsedPostAttributePictureType.PNG,
        ParsedPostAttributePictureType.JPEG,
        ParsedPostAttributePictureType.GIF,
        ParsedPostAttributePictureType.BMP,
        ParsedPostAttributePictureType.TIFF,
        ParsedPostAttributePictureType.MP4,
        ParsedPostAttributePictureType.WEBM,
    }.ToFrozenSet();

    protected static readonly FrozenDictionary<ParsedPostAttributePictureType, string> PictureTypeToExtensions = PictureTypes
        .ToDictionary(type => type, type => Enum.GetName(type).ToLowerInvariant())
        .ToFrozenDictionary();

    protected static readonly FrozenSet<string> AllowedMimeTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        MediaTypeNames.Image.Png,
        MediaTypeNames.Image.Jpeg,
        MediaTypeNames.Image.Gif,
        MediaTypeNames.Image.Bmp,
        MediaTypeNames.Image.Tiff,
        "video/mp4",
        "video/webm",
    }.ToFrozenSet();

    public async Task<Image<Rgb24>> DownloadAsync(ParsedPostAttributePicture picture, CancellationToken cancellationToken)
    {
        if (!PictureTypes.Contains(picture.ImageType))
            throw new ArgumentOutOfRangeException(nameof(picture), "Unsupported media type");

        try
        {
            await Semaphore.WaitAsync(cancellationToken);
            await Task.Delay(settings.Value.SubsequentCallDelay, cancellationToken);

            var url = $"{settings.Value.CdnHostName}/pics/post/picture-{picture.AttributeId}.{PictureTypeToExtensions[picture.ImageType]}";

            var context = ResilienceContextPool.Shared.Get(cancellationToken);
            context.Properties.Set(UrlKey, url);

            return await ResiliencePipeline.ExecuteAsync(
                async (ctx, state) => await DownloadAsync(state.url, state.picture, ctx.CancellationToken),
                context,
                (url, picture));
        }
        finally
        {
            Semaphore.Release();
        }
    }

    protected async Task<Image<Rgb24>> DownloadAsync(string url, ParsedPostAttributePicture picture, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        switch (response.StatusCode)
        {
            case HttpStatusCode.ServiceUnavailable:
                logger.LogWarning("CDN service unavailable. Making a pause.");
                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
                break;
        }

        response.EnsureSuccessStatusCode();

        var mimeType = response.Content.Headers.ContentType?.MediaType;
        if (!AllowedMimeTypes.Contains(mimeType))
        {
            logger.LogWarning("Unsupported {MimeType} MIME type received for {Url}.", mimeType, url);
            throw new NotSupportedException("Unsupported MIME type received.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var image = await mediaReducer.ReduceAsync(picture, stream, cancellationToken);
        return image;
    }
}

public interface IMediaDownloader
{
    Task<Image<Rgb24>> DownloadAsync(ParsedPostAttributePicture picture, CancellationToken cancellationToken);
}