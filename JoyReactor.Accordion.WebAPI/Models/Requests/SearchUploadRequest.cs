using System.ComponentModel.DataAnnotations;

namespace JoyReactor.Accordion.WebAPI.Models.Requests;

public record SearchUploadRequest
{
    [Required]
    public IFormFile Media { get; set; }
}