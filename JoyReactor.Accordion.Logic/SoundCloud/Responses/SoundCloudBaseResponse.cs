using JoyReactor.Accordion.Logic.SoundCloud.Responses.Models;
using System.Text.Json.Serialization;

namespace JoyReactor.Accordion.Logic.SoundCloud.Responses;

[JsonConverter(typeof(SoundCloudBaseJsonConverter))]
public abstract record SoundCloudBaseResponse : ISoundCloudResponse
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("kind")]
    public string? Kind { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("permalink")]
    public required string Permalink { get; init; }

    [JsonPropertyName("permalink_url")]
    public required string PermalinkUrl { get; init; }

    [JsonPropertyName("uri")]
    public string? Uri { get; init; }

    [JsonPropertyName("user_id")]
    public long UserId { get; init; }

    [JsonPropertyName("user")]
    public SoundCloudUser? User { get; init; }

    [JsonPropertyName("artwork_url")]
    public string? ArtworkUrl { get; init; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("last_modified")]
    public DateTime LastModified { get; init; }

    [JsonPropertyName("display_date")]
    public DateTime DisplayDate { get; init; }

    [JsonPropertyName("duration")]
    public long Duration { get; init; }

    [JsonPropertyName("embeddable_by")]
    public string? EmbeddableBy { get; init; }

    [JsonPropertyName("genre")]
    public string? Genre { get; init; }

    [JsonPropertyName("license")]
    public string? License { get; init; }

    [JsonPropertyName("likes_count")]
    public int LikesCount { get; init; }

    [JsonPropertyName("public")]
    public bool IsPublic { get; init; }

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

    [JsonPropertyName("tag_list")]
    public string? TagList { get; init; }
}

public interface ISoundCloudResponse
{
    public long Id { get; init; }

    public string? Kind { get; init; }

    public string Title { get; init; }

    public string Permalink { get; init; }

    public string PermalinkUrl { get; init; }

    public string? Uri { get; init; }

    public long UserId { get; init; }

    public SoundCloudUser? User { get; init; }

    public string? ArtworkUrl { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime LastModified { get; init; }

    public DateTime DisplayDate { get; init; }

    public long Duration { get; init; }

    public string? EmbeddableBy { get; init; }

    public string? Genre { get; init; }

    public string? License { get; init; }

    public int LikesCount { get; init; }

    public bool IsPublic { get; init; }

    public string? PurchaseTitle { get; init; }

    public string? PurchaseUrl { get; init; }

    public DateTime? ReleaseDate { get; init; }

    public int RepostsCount { get; init; }

    public string? SecretToken { get; init; }

    public string? Sharing { get; init; }

    public string? TagList { get; init; }
}