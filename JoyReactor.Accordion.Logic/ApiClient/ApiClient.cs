using GraphQL;
using GraphQL.Client.Abstractions;
using JoyReactor.Accordion.Logic.ApiClient.Models;
using System.Text;

namespace JoyReactor.Accordion.Logic.ApiClient;

public class ApiClient(IGraphQLClient graphQlClient)
    : IApiClient
{
    public Task<ApiClientResult<Post>> GetPostInformationAsync(int numberId, CancellationToken cancellationToken = default)
    {
        return GetPostInformationAsync(Convert.ToBase64String(Encoding.UTF8.GetBytes($"Post:{numberId}")), cancellationToken);
    }

    public async Task<ApiClientResult<Post>> GetPostInformationAsync(string nodeId, CancellationToken cancellationToken = default)
    {
        const string query = @"
            query($nodeId: ID!) {
              node(id: $nodeId) {
                id
                ... on Post {
                  id,
                  attributes {
                    id,
                    type,
                    image {
                      id
                    }
                  }
                }
              }
            }
        ";
        var request = new GraphQLRequest(query, new { nodeId });

        try
        {
            var response = await graphQlClient.SendQueryAsync<ApiClientResponse<Post>>(request, cancellationToken);
            return ApiClientResult<Post>.Success(response.Data.Node);
        }
        catch (Exception ex)
        {
            return ApiClientResult<Post>.Fail(ex);
        }
    }
}

public interface IApiClient
{
    Task<ApiClientResult<Post>> GetPostInformationAsync(int numberId, CancellationToken cancellationToken = default);
    Task<ApiClientResult<Post>> GetPostInformationAsync(string nodeId, CancellationToken cancellationToken = default);
}