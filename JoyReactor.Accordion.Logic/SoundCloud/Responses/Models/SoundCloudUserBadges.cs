using System.Text.Json.Serialization;

namespace JoyReactor.Accordion.Logic.SoundCloud.Responses.Models;

public record SoundCloudUserBadges
{
    [JsonPropertyName("pro")]
    public bool Pro { get; init; }

    [JsonPropertyName("creator_mid_tier")]
    public bool CreatorMidTier { get; init; }

    [JsonPropertyName("pro_unlimited")]
    public bool ProUnlimited { get; init; }

    [JsonPropertyName("verified")]
    public bool Verified { get; init; }
}