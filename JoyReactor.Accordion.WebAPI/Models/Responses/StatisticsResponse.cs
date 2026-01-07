namespace JoyReactor.Accordion.WebAPI.Models.Responses;

public record StatisticsResponse
{
    public int Vectors { get; set; }

    public int ParsedTags { get; set; }
    public int EmptyTags { get; set; }
    public int ParsedPosts { get; set; }

    public int ParsedPostAttributePictures { get; set; }
    public int ParsedPostAttributePicturesWithoutVector { get; set; }
    public int ParsedPostAttributePicturesWithVector { get; set; }

    public int ParsedPostAttributeEmbeds { get; set; }

    public int ParsedBandCamps { get; set; }
    public int ParsedCoubs { get; set; }
    public int ParsedSoundClouds { get; set; }
    public int ParsedVimeos { get; set; }
    public int ParsedYoutubes { get; set; }
}