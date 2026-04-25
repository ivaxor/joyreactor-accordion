namespace JoyReactor.Accordion.Logic.MQ.Messages;

public record VoteCreatedMessage
{
    public required Guid Id { get; init; }
}