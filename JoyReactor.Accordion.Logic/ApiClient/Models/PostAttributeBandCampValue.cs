using System.Text.Json.Serialization;

namespace JoyReactor.Accordion.Logic.ApiClient.Models;

public record PostAttributeBandCampValue
{
    [JsonPropertyName("url")]
    public string Url { get; set; }
}