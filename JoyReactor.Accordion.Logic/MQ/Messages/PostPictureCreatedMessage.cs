namespace JoyReactor.Accordion.Logic.MQ.Messages;

public record PostPictureCreatedMessage
{
    public required int AttributeId { get; init; }
}