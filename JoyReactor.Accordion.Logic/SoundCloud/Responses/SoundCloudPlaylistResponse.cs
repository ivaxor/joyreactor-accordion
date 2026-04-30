using JoyReactor.Accordion.Logic.SoundCloud.Responses.Models;
using System.Text.Json.Serialization;

namespace JoyReactor.Accordion.Logic.SoundCloud.Responses;

public record SoundCloudPlaylistResponse : SoundCloudBaseResponse
{
    [JsonPropertyName("managed_by_feeds")]
    public bool ManagedByFeeds { get; init; }

    [JsonPropertyName("set_type")]
    public string? SetType { get; init; }

    [JsonPropertyName("is_album")]
    public bool IsAlbum { get; init; }

    [JsonPropertyName("published_at")]
    public DateTime? PublishedAt { get; init; }

    [JsonPropertyName("tracks")]
    public required SoundCloudTrack[] Tracks { get; init; }

    [JsonPropertyName("track_count")]
    public int TrackCount { get; init; }
}