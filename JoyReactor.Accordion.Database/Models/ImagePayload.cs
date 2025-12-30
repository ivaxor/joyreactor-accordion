using System.Text.Json.Serialization;

namespace JoyReactor.Accordion.Database.Models;

public record ImagePayload
{
    [JsonPropertyName("postIds")]
    public HashSet<string> PostIds { get; set; }

    [JsonPropertyName("commentIds")]
    public HashSet<string> CommentIds { get; set; }

    [JsonPropertyName("imageIds")]
    public HashSet<string> ImageIds { get; set; }
}