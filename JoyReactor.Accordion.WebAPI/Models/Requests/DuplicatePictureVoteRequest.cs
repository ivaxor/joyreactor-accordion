using System.ComponentModel.DataAnnotations;

namespace JoyReactor.Accordion.WebAPI.Models.Requests;

public record DuplicatePictureVoteRequest
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    public bool Yes { get; set; }
}