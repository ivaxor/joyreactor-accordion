using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using JoyReactor.Accordion.Logic.Database.Sql.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace JoyReactor.Accordion.Logic.ApiClient;

public class ApiClientProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<ApiClientSettings> apiClientSettings,
    ILoggerFactory loggerFactory)
    : IApiClientProvider
{
    protected readonly ConcurrentDictionary<string, IApiClient> ApiClientByUrls = new ConcurrentDictionary<string, IApiClient>(StringComparer.OrdinalIgnoreCase);

    public IApiClient Provide(Api api)
    {
        return ApiClientByUrls.GetOrAdd(api.GraphQlEndpointUrl, (graphQlEndpointUrl) =>
        {
            var graphQlHttpClientOptions = new GraphQLHttpClientOptions
            {
                EndPoint = new Uri(graphQlEndpointUrl),
            };
            var httpClient = httpClientFactory.CreateClient($"{nameof(GraphQLHttpClient)}_{api.GraphQlEndpointUrl}");
            var graphQlHttpClient = new GraphQLHttpClient(graphQlHttpClientOptions, new SystemTextJsonSerializer(), httpClient);
            var apiClientLogger = loggerFactory.CreateLogger<ApiClient>();
            var apiClient = new ApiClient(graphQlHttpClient, apiClientSettings, apiClientLogger);
            return apiClient;
        });
    }
}

public interface IApiClientProvider
{
    IApiClient Provide(Api api);
}