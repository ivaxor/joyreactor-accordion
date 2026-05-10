using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace JoyReactor.Accordion.WebAPI.Models.Requests;

public record SearchUploadRequest
{
    [Required]
    public IFormFile Media { get; set; }

    [Required]
#if !DEBUG
    [Range(1, 5)]
#else
    [Range(1, 100)]
#endif
    [DefaultValue(3)]
    public int Limit { get; set; }

    [Required]
#if !DEBUG
    [Range(0.8, 1.0)]
#else
    [Range(0.0, 1.0)]
#endif
    [DefaultValue(0.95f)]
    public float Threshold { get; set; }
}