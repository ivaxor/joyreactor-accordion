using JoyReactor.Accordion.Logic.ApiClient.Models;

namespace JoyReactor.Accordion.Logic.MQ.Messages;

public record ApiPostCreatedMessage
{
    public required Guid ApiId { get; init; }
    public required Post Post { get; init; }
}