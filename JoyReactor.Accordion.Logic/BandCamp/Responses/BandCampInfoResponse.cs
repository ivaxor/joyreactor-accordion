using System.Text.Json.Serialization;

namespace JoyReactor.Accordion.Logic.BandCamp.Responses;

public record BandCampInfoResponse
{
    [JsonPropertyName("item_type")]
    public required string Type { get; init; }

    [JsonPropertyName("item_id")]
    public required long Id { get; init; }
}