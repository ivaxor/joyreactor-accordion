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

    protected readonly SemaphoreSlim Semaphore = new SemaphoreSlim(settings.Value.BatchSize, settings.Value.BatchSize);

    protected readonly ResiliencePipeline ResiliencePipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            ShouldHandle = new PredicateBuilder()
                .Handle<TimeoutRejectedException>()
                .Handle<HttpRequestException>(),
            MaxRetryAttempts = Math.Max(settings.Value.CdnHostNames.Length - 1, settings.Value.MaxRetryAttempts),
            Delay = settings.Value.RetryDelay,
            BackoffType = DelayBackoffType.Linear,
            UseJitter = true,
            OnRetry = async args =>
            {
                var maxRetryAttrempts = Math.Max(settings.Value.CdnHostNames.Length, settings.Value.MaxRetryAttempts);

                args.Context.Properties.TryGetValue(UrlKey, out var url);

                switch (args.Outcome.Exception)
                {
                    case HttpRequestException ex:
                        var isDnsIssues =
                        ex.Message.StartsWith("No such host is known.", StringComparison.Ordinal) ||
                        ex.Message.StartsWith("Name or service not known", StringComparison.Ordinal) ||
                        ex.Message.StartsWith("The requested name is valid, but no data of the requested type was found.", StringComparison.Ordinal);

                        if (isDnsIssues)
                            logger.LogWarning("Failed to download media from {Url} due to DNS issues. Attempt: {Attempt}/{MaxAttempts}. ", url, args.AttemptNumber + 1, maxRetryAttrempts);
                        else if (ex.StatusCode != null)
                        {
                            switch (ex.StatusCode)
                            {
                                case HttpStatusCode.ServiceUnavailable:
                                    logger.LogWarning("Failed to download media from {Url} due to unavailable CDN service. Attempt: {Attempt}/{MaxAttempts}. Making a pause.", url, args.AttemptNumber + 1, maxRetryAttrempts);
                                    await Task.Delay(TimeSpan.FromMinutes(1), args.Context.CancellationToken);
                                    break;

                                default:
                                    logger.LogWarning("Failed to download media from {Url} due to unsuccesfull status code. Status code: {StatusCode}. Attempt: {Attempt}/{MaxAttempts}. ", url, ex.StatusCode, args.AttemptNumber + 1, maxRetryAttrempts);
                                    break;
                            }
                        }
                        else
                            logger.LogWarning(ex, "Failed to download media from {Url}. Attempt: {Attempt}/{MaxAttempts}. ", url, args.AttemptNumber + 1, maxRetryAttrempts);
                        break;

                    default:
                        logger.LogWarning(args.Outcome.Exception, "Failed to download media from {Url}. Attempt: {Attempt}/{MaxAttempts}.", url, args.AttemptNumber + 1, maxRetryAttrempts);
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
        ParsedPostAttributePictureType.WEBP,
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
        MediaTypeNames.Image.Webp,
    }.ToFrozenSet();

    public async Task<Image<Rgb24>> DownloadReducedAsync(ParsedPostAttributePicture picture, CancellationToken cancellationToken)
    {
        if (!PictureTypes.Contains(picture.ImageType))
            throw new ArgumentOutOfRangeException(nameof(picture), "Unsupported media type");

        try
        {
            await Semaphore.WaitAsync(cancellationToken);
            await Task.Delay(settings.Value.SubsequentCallDelay, cancellationToken);

            var initialOffset = Random.Shared.Next(0, settings.Value.CdnHostNames.Length);
            var retryOffset = 0;

            return await ResiliencePipeline.ExecuteAsync(
                async (ResilienceContext context, ParsedPostAttributePicture state) =>
                {
                    var index = (initialOffset + retryOffset) % settings.Value.CdnHostNames.Length;
                    var cdnHostName = settings.Value.CdnHostNames[index];
                    var url = $"{cdnHostName}/pics/post/picture-{state.AttributeId}.{PictureTypeToExtensions[state.ImageType]}";

                    context.Properties.Set(UrlKey, url);
                    retryOffset++;

                    return await DownloadAndReduceAsync(url, state, context.CancellationToken);
                },
                ResilienceContextPool.Shared.Get(cancellationToken),
                picture);
        }
        finally
        {
            Semaphore.Release();
        }
    }

    public async Task<Stream> DownloadRawAsync(ParsedPostAttributePicture picture, CancellationToken cancellationToken)
    {
        if (!PictureTypes.Contains(picture.ImageType))
            throw new ArgumentOutOfRangeException(nameof(picture), "Unsupported media type");

        try
        {
            await Semaphore.WaitAsync(cancellationToken);
            await Task.Delay(settings.Value.SubsequentCallDelay, cancellationToken);

            var initialOffset = Random.Shared.Next(0, settings.Value.CdnHostNames.Length);
            var retryOffset = 0;

            return await ResiliencePipeline.ExecuteAsync(
                async (ResilienceContext context, ParsedPostAttributePicture state) =>
                {
                    var index = (initialOffset + retryOffset) % settings.Value.CdnHostNames.Length;
                    var cdnHostName = settings.Value.CdnHostNames[index];
                    var url = $"{cdnHostName}/pics/post/picture-{state.AttributeId}.{PictureTypeToExtensions[state.ImageType]}";

                    context.Properties.Set(UrlKey, url);
                    retryOffset++;

                    return await DownloadRawAsync(url, context.CancellationToken);
                },
                ResilienceContextPool.Shared.Get(cancellationToken),
                picture);
        }
        finally
        {
            Semaphore.Release();
        }
    }

    protected async Task<Image<Rgb24>> DownloadAndReduceAsync(string url, ParsedPostAttributePicture picture, CancellationToken cancellationToken)
    {
        await using var stream = await DownloadRawAsync(url, cancellationToken);
        var image = await mediaReducer.ReduceAsync(picture, stream, cancellationToken);
        return image;
    }

    protected async Task<Stream> DownloadRawAsync(string url, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var mimeType = response.Content.Headers.ContentType?.MediaType;
        if (!AllowedMimeTypes.Contains(mimeType))
        {
            logger.LogWarning("Unsupported {MimeType} MIME type received for {Url}.", mimeType, url);
            throw new NotSupportedException("Unsupported MIME type received.");
        }

        var memoryStream = new MemoryStream();
        await response.Content.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        return memoryStream;
    }
}

public interface IMediaDownloader
{
    Task<Image<Rgb24>> DownloadReducedAsync(ParsedPostAttributePicture picture, CancellationToken cancellationToken);
    Task<Stream> DownloadRawAsync(ParsedPostAttributePicture picture, CancellationToken cancellationToken);
}