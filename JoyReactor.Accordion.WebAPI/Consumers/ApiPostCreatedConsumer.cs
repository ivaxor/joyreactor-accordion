using JoyReactor.Accordion.Logic.MQ.Messages;
using JoyReactor.Accordion.Logic.Parsers;
using MassTransit;

namespace JoyReactor.Accordion.WebAPI.Consumers;

public class ApiPostCreatedConsumer(
    IPostParser postParser,
    IPublishEndpoint publishEndpoint)
    : IConsumer<ApiPostCreatedMessage>
{
    public async Task Consume(ConsumeContext<ApiPostCreatedMessage> context)
    {
        var result = await postParser.ParseAsync(context.Message, context.CancellationToken);
        if (result == null)
            return;

        var messages = result.PostAttributePictureNumberIds
            .Select(id => new PostPictureCreatedMessage() { AttributeId = id })
            .ToArray();

        await publishEndpoint.PublishBatch(messages, context.CancellationToken);
    }
}

public class ApiPostCreatedConsumerDefinition : ConsumerDefinition<ApiPostCreatedConsumer>
{
    public ApiPostCreatedConsumerDefinition()
    {
        EndpointName = "api_post_created";
        ConcurrentMessageLimit = 1;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<ApiPostCreatedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retryConfurator => retryConfurator.Interval(3, TimeSpan.FromSeconds(5)));
    }
}