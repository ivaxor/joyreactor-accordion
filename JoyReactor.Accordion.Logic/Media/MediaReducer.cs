using FFMpegCore;
using FFMpegCore.Pipes;
using JoyReactor.Accordion.Logic.Database.Sql.Entities;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Net.Mime;

namespace JoyReactor.Accordion.Logic.Media;

public class MediaReducer(IOptions<MediaSettings> settings)
    : IMediaReducer
{
    protected readonly ResizeOptions ResizeOptions = new()
    {
        Size = new Size(settings.Value.ResizedSize, settings.Value.ResizedSize),
        Mode = ResizeMode.Pad,
        Sampler = KnownResamplers.Lanczos3,
    };

    public Task<Image<Rgb24>> ReduceAsync(ParsedPostAttributePicture picture, Stream stream, CancellationToken cancellationToken)
    {
        switch (picture.ImageType)
        {
            case ParsedPostAttributePictureType.PNG:
            case ParsedPostAttributePictureType.JPEG:
            case ParsedPostAttributePictureType.GIF:
            case ParsedPostAttributePictureType.BMP:
            case ParsedPostAttributePictureType.TIFF:
                return ReducePictureAsync(stream, cancellationToken);

            case ParsedPostAttributePictureType.MP4:
            case ParsedPostAttributePictureType.WEBM:
                return ReduceVideoAsync(stream, cancellationToken);

            default:
                throw new NotImplementedException();
        }
    }

    public Task<Image<Rgb24>> ReduceAsync(string mimeType, Stream stream, CancellationToken cancellationToken)
    {
        switch (mimeType)
        {
            case MediaTypeNames.Image.Png:
            case MediaTypeNames.Image.Jpeg:
            case MediaTypeNames.Image.Gif:
            case MediaTypeNames.Image.Bmp:
            case MediaTypeNames.Image.Tiff:
                return ReducePictureAsync(stream, cancellationToken);

            case "video/mp4":
            case "video/webm":
                return ReduceVideoAsync(stream, cancellationToken);

            default:
                throw new NotImplementedException();
        }
    }

    protected async Task<Image<Rgb24>> ReducePictureAsync(Stream stream, CancellationToken cancellationToken)
    {
        var image = await Image.LoadAsync<Rgb24>(stream, cancellationToken);
        if (image.Frames.Count <= 1)
        {
            image.Mutate(x => x.Resize(ResizeOptions));
            return image;
        }

        try
        {
            var frameIndex = Convert.ToInt32(Math.Round(image.Frames.Count / 2.0));
            if (frameIndex >= image.Frames.Count)
                frameIndex = image.Frames.Count - 1;

            var singleFrameImage = image.Frames.CloneFrame(frameIndex);
            singleFrameImage.Mutate(x => x.Resize(ResizeOptions));

            return singleFrameImage;
        }
        finally
        {
            image.Dispose();
        }
    }

    protected async Task<Image<Rgb24>> ReduceVideoAsync(Stream stream, CancellationToken cancellationToken)
    {
        try
        {
            await using var inputStream = new MemoryStream();
            await stream.CopyToAsync(inputStream, cancellationToken);
            inputStream.Position = 0;

            var mediaAnalysis = await FFProbe.AnalyseAsync(inputStream, cancellationToken: cancellationToken);
            var seekTo = mediaAnalysis.Duration.TotalSeconds > 0
                ? TimeSpan.FromTicks(Convert.ToInt32(Math.Round(mediaAnalysis.Duration.Ticks / 2.0)))
                : TimeSpan.Zero;
            inputStream.Position = 0;

            await using var outputStream = new MemoryStream();
            await FFMpegArguments
                .FromPipeInput(new StreamPipeSource(inputStream))
                .OutputToPipe(new StreamPipeSink(outputStream), options => options
                    .Seek(seekTo)
                    .WithFrameOutputCount(1)
                    .WithVideoCodec("mjpeg")
                    .ForceFormat("image2"))
                .ProcessAsynchronously();
            outputStream.Position = 0;

            var image = await Image.LoadAsync<Rgb24>(outputStream, cancellationToken);
            image.Mutate(x => x.Resize(ResizeOptions));
            return image;
        }
        catch (Exception ex)
        {
            throw;
        }
    }
}

public interface IMediaReducer
{
    Task<Image<Rgb24>> ReduceAsync(ParsedPostAttributePicture picture, Stream stream, CancellationToken cancellationToken);
    Task<Image<Rgb24>> ReduceAsync(string mimeType, Stream stream, CancellationToken cancellationToken);
}