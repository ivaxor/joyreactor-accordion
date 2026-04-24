using JoyReactor.Accordion.Logic.MQ.Messages;
using JoyReactor.Accordion.Logic.Parsers;
using MassTransit;

namespace JoyReactor.Accordion.WebAPI.Consumers;

public class ApiPostConsumer(IPostParser postParser)
    : IConsumer<ApiPostMessage>
{
    public async Task Consume(ConsumeContext<ApiPostMessage> context)
    {
        await postParser.ParseAsync(context.Message, context.CancellationToken);
    }
}

public class ApiPostConsumerDefinition : ConsumerDefinition<ApiPostConsumer>
{
    public ApiPostConsumerDefinition()
    {
        EndpointName = "api_post";
        ConcurrentMessageLimit = 1;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<ApiPostConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retryConfurator => retryConfurator.Interval(3, TimeSpan.FromSeconds(5)));
    }
}