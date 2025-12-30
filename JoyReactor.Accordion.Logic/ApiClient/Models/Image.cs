using JoyReactor.Accordion.Logic.ApiClient;
using System.Text.Json.Serialization;

namespace JoyReactor.Accordion.Logic.ApiClient.Models;

public record Image : NodeResponseObject
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("hasVideo")]
    public bool? HasVideo { get; set; }

    [JsonPropertyName("width")]
    public int? Width { get; set; }

    [JsonPropertyName("height")]
    public int? Height { get; set; }
}