using JoyReactor.Accordion.Logic.Extensions;

namespace JoyReactor.Accordion.WebAPI.Models.Responses;

public record SearchEmbeddedResponse
{
    public SearchEmbeddedResponse() { }
    public SearchEmbeddedResponse(IEnumerable<Guid> postIds, string? externalId = null)
    {
        PostIds = postIds.Select(postId => postId.ToInt()).ToArray();
        ExternalId = externalId;
    }

    public int[] PostIds { get; set; }
    public string? ExternalId { get; set; }
}