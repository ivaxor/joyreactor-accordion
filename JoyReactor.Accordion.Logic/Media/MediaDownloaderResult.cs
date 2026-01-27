using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Net;

namespace JoyReactor.Accordion.Logic.Media;

public record MediaDownloaderResult
{
    public bool IsSuccess { get; set; }
    public HttpStatusCode HttpStatusCode { get; set; }

    public Image<Rgb24>? Image { get; set; }    
    public Exception? Exception { get; set; }

    public MediaDownloaderResult()
    {
        
    }

    public MediaDownloaderResult(HttpStatusCode httpStatusCode, Image<Rgb24> image)
    {
        IsSuccess = true;
        HttpStatusCode = httpStatusCode;
        Image = image;
    }

    public MediaDownloaderResult(HttpStatusCode httpStatusCode)
    {
        IsSuccess = false;
        HttpStatusCode = httpStatusCode;
    }
}