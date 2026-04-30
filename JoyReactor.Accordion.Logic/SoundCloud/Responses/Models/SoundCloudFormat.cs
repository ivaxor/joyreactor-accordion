using System.Text.Json.Serialization;

namespace JoyReactor.Accordion.Logic.SoundCloud.Responses.Models;

public record SoundCloudFormat
{
    [JsonPropertyName("protocol")]
    public string? Protocol { get; init; }

    [JsonPropertyName("mime_type")]
    public string? MimeType { get; init; }
}
