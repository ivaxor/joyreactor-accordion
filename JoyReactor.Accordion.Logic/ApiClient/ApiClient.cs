using GraphQL;
using GraphQL.Client.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace JoyReactor.Accordion.Logic.ApiClient;

public class ApiClient(
    IGraphQLClient graphQlClient,
    IOptions<ApiClientSettings> settings,
    ILogger<ApiClient> logger)
    : IApiClient
{
    internal static SemaphoreSlim semaphore = new SemaphoreSlim(0, 1);

    internal readonly ResiliencePipeline resiliencePipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            ShouldHandle = new PredicateBuilder()
                .Handle<HttpRequestException>()
                .Handle<TimeoutException>(),
            MaxRetryAttempts = settings.Value.MaxRetryAttempts,
            Delay = settings.Value.SubsequentCallDelay,
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            OnRetry = args =>
            {
                logger.LogWarning("Failed to send GraphQL request to API. Message: {Message}. Attempt: {Attempt}", args.Outcome.Exception?.Message, args.AttemptNumber);
                return default;
            }
        })
        .AddTimeout(TimeSpan.FromSeconds(10))
        .Build();

    public async Task<ApiClientResponse<T>> SendQueryAsync<T>(GraphQLRequest request, CancellationToken cancellationToken = default)
        where T : NodeResponseObject
    {
        await semaphore.WaitAsync(cancellationToken);

        try
        {
            return await resiliencePipeline.ExecuteAsync(async ct =>
            {
                var response = await graphQlClient.SendQueryAsync<ApiClientResponse<T>>(request, ct);
                foreach (var error in response.Errors ?? [])
                    logger.LogError("Failed response from GraphQL API recieved. Message: {Message}", error.Message);

                return response.Data;
            }, cancellationToken);
        }
        catch
        {
            throw;
        }
        finally
        {
            semaphore.Release();
        }
    }
}

public interface IApiClient
{
    Task<ApiClientResponse<T>> SendQueryAsync<T>(GraphQLRequest request, CancellationToken cancellationToken = default)
        where T : NodeResponseObject;
}