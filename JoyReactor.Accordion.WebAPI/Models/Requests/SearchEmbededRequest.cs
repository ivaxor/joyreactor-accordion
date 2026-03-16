using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace JoyReactor.Accordion.WebAPI.Models.Requests;

public record SearchEmbeddedRequest
{
    [Required]
    public SearchEmbeddedType Type { get; set; }

    [Required]
    [MaxLength(100)]
    public string Text { get; set; }

    [Required]
    [Range(1, 100)]
    [DefaultValue(3)]
    public int Limit { get; set; }
}

public enum SearchEmbeddedType
{
    BandCamp,
    Coub,
    SoundCloud,
    Vimeo,
    YouTube,
}