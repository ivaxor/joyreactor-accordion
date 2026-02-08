using System.ComponentModel.DataAnnotations;

namespace JoyReactor.Accordion.WebAPI.Models.Requests;

public record SearchDownloadRequest : IValidatableObject
{
    [Required]
    [MaxLength(500)]
    public string MediaUrl { get; set; }

    [Required]
    [Range(0.8, 1)]
    public float Threshold { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var mediaUri = new Uri(MediaUrl);
        if (!mediaUri.IsAbsoluteUri)
            yield return new ValidationResult("URL must be absolute");

        if (mediaUri.Scheme != Uri.UriSchemeHttps)
            yield return new ValidationResult("Only HTTPS protocol is allowed");

        var host = mediaUri.Host;
        if (Uri.CheckHostName(host) is UriHostNameType.IPv4 or UriHostNameType.IPv6)
            yield return new ValidationResult("IP addresses are not allowed");

        if (!host.Contains('.', StringComparison.OrdinalIgnoreCase))
            yield return new ValidationResult("Top level domain required");
    }
}
