
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace JoyReactor.Accordion.Logic.Image.Downloader;

public record ImageDownloaderResult : IDisposable
{
    public bool IsSuccess { get; set; }
    public Exception Exception { get; set; }
    public string ErrorMessage { get; set; }

    public Image<Rgb24> Value { get; set; }

    public static ImageDownloaderResult Success(Image<Rgb24> value)
    {
        return new ImageDownloaderResult
        {
            IsSuccess = true,
            Value = value,
        };
    }

    public static ImageDownloaderResult Fail(string errorMessage)
    {
        return new ImageDownloaderResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
        };
    }

    public static ImageDownloaderResult Fail(Exception exception)
    {
        return new ImageDownloaderResult
        {
            IsSuccess = false,
            ErrorMessage = exception.Message,
            Exception = exception,
        };
    }

    public void Dispose()
    {
        Value.Dispose();
    }
}