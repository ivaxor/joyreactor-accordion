using JoyReactor.Accordion.Logic.ApiClient.Models;
using System.Text.Json.Serialization;

namespace JoyReactor.Accordion.Logic.ApiClient.Responses;

public record ChangedPostsResponse
{
    [JsonPropertyName("changedPosts")]
    public Post[] ChangedPosts { get; set; }
}