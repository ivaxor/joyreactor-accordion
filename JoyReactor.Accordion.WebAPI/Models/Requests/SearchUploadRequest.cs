using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace JoyReactor.Accordion.WebAPI.Models.Requests;

public record SearchUploadRequest
{
    [Required]
    public IFormFile Media { get; set; }

    [Required]
    [Range(1, 5)]
    [DefaultValue(3)]
    public int Limit { get; set; }

    [Required]
    [Range(0.8, 1)]
    [DefaultValue(0.95f)]
    public float Threshold { get; set; }
}