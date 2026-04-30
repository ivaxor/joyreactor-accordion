using System.Text.Json.Serialization;

namespace JoyReactor.Accordion.Logic.SoundCloud.Responses.Models;

public record SoundCloudTranscoding
{
    [JsonPropertyName("url")]
    public string? Url { get; init; }

    [JsonPropertyName("preset")]
    public string? Preset { get; init; }

    [JsonPropertyName("duration")]
    public long Duration { get; init; }

    [JsonPropertyName("snipped")]
    public bool Snipped { get; init; }

    [JsonPropertyName("format")]
    public SoundCloudFormat? Format { get; init; }

    [JsonPropertyName("quality")]
    public string? Quality { get; init; }

    [JsonPropertyName("is_legacy_transcoding")]
    public bool IsLegacyTranscoding { get; init; }
}
