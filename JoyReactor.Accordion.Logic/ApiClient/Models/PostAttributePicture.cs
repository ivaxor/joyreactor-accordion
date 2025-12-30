using JoyReactor.Accordion.Logic.ApiClient;
using System.Text.Json.Serialization;

namespace JoyReactor.Accordion.Logic.ApiClient.Models;

public record PostAttributePicture : NodeResponseObject
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("image")]
    public Image? Image { get; set; }
}