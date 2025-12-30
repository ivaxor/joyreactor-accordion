using JoyReactor.Accordion.Logic.ApiClient.Models;
using Microsoft.Extensions.Options;

namespace JoyReactor.Accordion.Logic.Media.Images;

public class ImageDownloader(
    HttpClient httpClient,
    IImageReducer imageReducer,
    IOptions<ImageSettings> settings)
    : IImageDownloader
{
    public Task<ImageDownloaderResult> DownloadAsync(PostAttribute postAttribute, CancellationToken cancellationToken = default)
    {
        return postAttribute.Type switch
        {
            "PICTURE" => DownloadAsync($"pics/post/picture-{postAttribute.NumberId}.{postAttribute.Image!.Type}", cancellationToken),
            _ => Task.FromResult(ImageDownloaderResult.Fail("Invalid type"))
        };
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
    Task<ImageDownloaderResult> DownloadAsync(PostAttribute postAttribute, CancellationToken cancellationToken = default);
}