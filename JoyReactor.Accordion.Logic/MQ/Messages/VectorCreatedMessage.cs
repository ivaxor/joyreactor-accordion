namespace JoyReactor.Accordion.Logic.MQ.Messages;

public record VectorCreatedMessage
{
    public required int AttributeId { get; init; }
}