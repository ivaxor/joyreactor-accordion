using JoyReactor.Accordion.Logic.ApiClient.Models;
using JoyReactor.Accordion.Logic.Image.Reducer;
using Microsoft.Extensions.Options;

namespace JoyReactor.Accordion.Logic.Image.Downloader;

public class ImageDownloader(
    HttpClient httpClient,
    IImageReducer imageReducer,
    IOptions<ImageSettings> settings)
    : IImageDownloader
{
    public Task<ImageDownloaderResult> DownloadAsync(PostAttributePicture postAttributePicture, CancellationToken cancellationToken = default)
    {
        var path = $"pics/post/picture-{postAttributePicture.NumberId}.{postAttributePicture.Image!.Type}";
        return DownloadAsync(path, cancellationToken);
    }

    internal async Task<ImageDownloaderResult> DownloadAsync(string path, CancellationToken cancellationToken = default)
    {
        foreach (var cdnDomainName in settings.Value.CdnDomainNames)
        {
            var url = $"{cdnDomainName}/{path}";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Referer", "https://joyreactor.cc");

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                continue;

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var image = await imageReducer.ReduceAsync(stream, cancellationToken);
            return ImageDownloaderResult.Success(image);
        }

        throw new NotImplementedException();
    }
}

public interface IImageDownloader
{
    Task<ImageDownloaderResult> DownloadAsync(PostAttributePicture postAttributePicture, CancellationToken cancellationToken = default);
}