using System.Text.Json.Serialization;

namespace JoyReactor.Accordion.Logic.SoundCloud.Responses.Models;

public record SoundCloudMedia
{
    [JsonPropertyName("transcodings")]
    public required SoundCloudTranscoding[] Transcodings { get; init; }
}
