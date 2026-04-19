using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace JoyReactor.Accordion.WebAPI.Models.Requests;

public record DuplicatePictureTelegramVoteRequest
{
    [Required]
    [JsonPropertyName("dpi")]
    public Guid DuplicatePictureId { get; set; }

    [Required]
    [JsonPropertyName("y")]
    public bool Yes { get; set; }
}