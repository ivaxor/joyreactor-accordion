using JoyReactor.Accordion.Logic.SoundCloud.Responses.Models;
using System.Text.Json.Serialization;

namespace JoyReactor.Accordion.Logic.SoundCloud.Responses;

public record SoundCloudTrackResponse : SoundCloudBaseResponse
{
    [JsonPropertyName("caption")]
    public string? Caption { get; init; }

    [JsonPropertyName("commentable")]
    public bool Commentable { get; init; }

    [JsonPropertyName("comment_count")]
    public int CommentCount { get; init; }

    [JsonPropertyName("downloadable")]
    public bool Downloadable { get; init; }

    [JsonPropertyName("download_count")]
    public int DownloadCount { get; init; }

    [JsonPropertyName("full_duration")]
    public long FullDuration { get; init; }

    [JsonPropertyName("has_downloads_left")]
    public bool HasDownloadsLeft { get; init; }

    [JsonPropertyName("label_name")]
    public string? LabelName { get; init; }

    [JsonPropertyName("playback_count")]
    public int PlaybackCount { get; init; }

    [JsonPropertyName("publisher_metadata")]
    public SoundCloudPublisherMetadata? PublisherMetadata { get; init; }

    [JsonPropertyName("state")]
    public string? State { get; init; }

    [JsonPropertyName("streamable")]
    public bool Streamable { get; init; }

    [JsonPropertyName("urn")]
    public string? Urn { get; init; }

    [JsonPropertyName("waveform_url")]
    public string? WaveformUrl { get; init; }

    [JsonPropertyName("media")]
    public SoundCloudMedia? Media { get; init; }

    [JsonPropertyName("station_urn")]
    public string? StationUrn { get; init; }

    [JsonPropertyName("station_permalink")]
    public string? StationPermalink { get; init; }

    [JsonPropertyName("track_authorization")]
    public string? TrackAuthorization { get; init; }

    [JsonPropertyName("monetization_model")]
    public string? MonetizationModel { get; init; }

    [JsonPropertyName("policy")]
    public string? Policy { get; init; }
}