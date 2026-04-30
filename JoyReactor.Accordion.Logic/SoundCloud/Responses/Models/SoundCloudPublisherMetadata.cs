using System.Text.Json.Serialization;

namespace JoyReactor.Accordion.Logic.SoundCloud.Responses.Models;

public record SoundCloudPublisherMetadata
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("urn")]
    public string? Urn { get; init; }

    [JsonPropertyName("artist")]
    public string? Artist { get; init; }

    [JsonPropertyName("contains_music")]
    public bool ContainsMusic { get; init; }

    [JsonPropertyName("release_title")]
    public string? ReleaseTitle { get; init; }
}
