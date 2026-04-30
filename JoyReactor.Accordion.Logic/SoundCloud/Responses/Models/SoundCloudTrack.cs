using System.Text.Json.Serialization;

namespace JoyReactor.Accordion.Logic.SoundCloud.Responses.Models;

public record SoundCloudTrack
{
    [JsonPropertyName("artwork_url")]
    public string? ArtworkUrl { get; init; }

    [JsonPropertyName("caption")]
    public string? Caption { get; init; }

    [JsonPropertyName("commentable")]
    public bool Commentable { get; init; }

    [JsonPropertyName("comment_count")]
    public int CommentCount { get; init; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("downloadable")]
    public bool Downloadable { get; init; }

    [JsonPropertyName("download_count")]
    public int DownloadCount { get; init; }

    [JsonPropertyName("duration")]
    public long Duration { get; init; }

    [JsonPropertyName("full_duration")]
    public long FullDuration { get; init; }

    [JsonPropertyName("embeddable_by")]
    public string? EmbeddableBy { get; init; }

    [JsonPropertyName("genre")]
    public string? Genre { get; init; }

    [JsonPropertyName("has_downloads_left")]
    public bool HasDownloadsLeft { get; init; }

    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("kind")]
    public string? Kind { get; init; }

    [JsonPropertyName("label_name")]
    public string? LabelName { get; init; }

    [JsonPropertyName("last_modified")]
    public DateTime LastModified { get; init; }

    [JsonPropertyName("license")]
    public string? License { get; init; }

    [JsonPropertyName("likes_count")]
    public int LikesCount { get; init; }

    [JsonPropertyName("permalink")]
    public string? Permalink { get; init; }

    [JsonPropertyName("permalink_url")]
    public string? PermalinkUrl { get; init; }

    [JsonPropertyName("playback_count")]
    public int PlaybackCount { get; init; }

    [JsonPropertyName("public")]
    public bool IsPublic { get; init; }

    [JsonPropertyName("publisher_metadata")]
    public SoundCloudPublisherMetadata? PublisherMetadata { get; init; }

    [JsonPropertyName("purchase_title")]
    public string? PurchaseTitle { get; init; }

    [JsonPropertyName("purchase_url")]
    public string? PurchaseUrl { get; init; }

    [JsonPropertyName("release_date")]
    public DateTime? ReleaseDate { get; init; }

    [JsonPropertyName("reposts_count")]
    public int RepostsCount { get; init; }

    [JsonPropertyName("secret_token")]
    public string? SecretToken { get; init; }

    [JsonPropertyName("sharing")]
    public string? Sharing { get; init; }

    [JsonPropertyName("state")]
    public string? State { get; init; }

    [JsonPropertyName("streamable")]
    public bool Streamable { get; init; }

    [JsonPropertyName("tag_list")]
    public string? TagList { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("uri")]
    public string? Uri { get; init; }

    [JsonPropertyName("urn")]
    public string? Urn { get; init; }

    [JsonPropertyName("user_id")]
    public long UserId { get; init; }

    [JsonPropertyName("visuals")]
    public object? Visuals { get; init; }

    [JsonPropertyName("waveform_url")]
    public string? WaveformUrl { get; init; }

    [JsonPropertyName("display_date")]
    public DateTime DisplayDate { get; init; }

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

    [JsonPropertyName("user")]
    public SoundCloudUser? User { get; init; }
}
