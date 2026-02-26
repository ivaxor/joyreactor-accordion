namespace JoyReactor.Accordion.WebAPI.Models.Requests;

public record DuplicatePictureVoteRequest
{
    public Guid Id { get; set; }
    public bool Yes { get; set; }
}