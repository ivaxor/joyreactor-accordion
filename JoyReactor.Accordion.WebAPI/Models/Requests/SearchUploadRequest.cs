using System.ComponentModel.DataAnnotations;

namespace JoyReactor.Accordion.WebAPI.Models.Requests;

public record SearchUploadRequest
{
    [Required]
    public IFormFile Media { get; set; }

    [Required]
    [Range(0.8, 1)]
    public float Threshold { get; set; }
}