using System.Text.Json.Serialization;

namespace JoyReactor.Accordion.Logic.SoundCloud.Responses.Models;

public record SoundCloudUser
{
    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; init; }

    [JsonPropertyName("first_name")]
    public string? FirstName { get; init; }

    [JsonPropertyName("followers_count")]
    public int FollowersCount { get; init; }

    [JsonPropertyName("full_name")]
    public string? FullName { get; init; }

    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("kind")]
    public string? Kind { get; init; }

    [JsonPropertyName("last_modified")]
    public DateTime LastModified { get; init; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; init; }

    [JsonPropertyName("permalink")]
    public required string Permalink { get; init; }

    [JsonPropertyName("permalink_url")]
    public required string PermalinkUrl { get; init; }

    [JsonPropertyName("uri")]
    public string? Uri { get; init; }

    [JsonPropertyName("urn")]
    public string? Urn { get; init; }

    [JsonPropertyName("username")]
    public required string Username { get; init; }

    [JsonPropertyName("verified")]
    public bool Verified { get; init; }

    [JsonPropertyName("city")]
    public string? City { get; init; }

    [JsonPropertyName("country_code")]
    public string? CountryCode { get; init; }

    [JsonPropertyName("badges")]
    public SoundCloudUserBadges? Badges { get; init; }

    [JsonPropertyName("station_urn")]
    public string? StationUrn { get; init; }

    [JsonPropertyName("station_permalink")]
    public string? StationPermalink { get; init; }
}
