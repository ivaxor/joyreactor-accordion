using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace JoyReactor.Accordion.Logic.Image.Reducer;

public class ImageReducer(IOptions<ImageSettings> settings)
    : IImageReducer
{
    internal readonly ResizeOptions ResizeOptions = new()
    {
        Size = new Size(settings.Value.ResizedSize, settings.Value.ResizedSize),
        Mode = ResizeMode.Pad,
        Sampler = KnownResamplers.Lanczos3,
    };

    public async Task<Image<Rgb24>> ReduceAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var image = await SixLabors.ImageSharp.Image.LoadAsync<Rgb24>(stream, cancellationToken);
        image.Mutate(x => x.Resize(ResizeOptions));

        return image;
    }
}

public interface IImageReducer
{
    Task<Image<Rgb24>> ReduceAsync(Stream stream, CancellationToken cancellationToken = default);
}