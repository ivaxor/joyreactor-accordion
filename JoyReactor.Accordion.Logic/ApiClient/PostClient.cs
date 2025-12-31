using GraphQL;
using GraphQL.Client.Abstractions;
using JoyReactor.Accordion.Logic.ApiClient.Models;
using System.Text;

namespace JoyReactor.Accordion.Logic.ApiClient;

public class PostClient(IGraphQLClient graphQlClient)
    : IPostClient
{
    public Task<ApiClientResult<Post>> GetAsync(int numberId, CancellationToken cancellationToken = default)
    {
        return GetAsync(Convert.ToBase64String(Encoding.UTF8.GetBytes($"Post:{numberId}")), cancellationToken);
    }

    public async Task<ApiClientResult<Post>> GetAsync(string nodeId, CancellationToken cancellationToken = default)
    {
        const string query = @"
            query PostClient_GetAsync($nodeId: ID!) {
                node(id: $nodeId) {
                    id
                    ... on Post {
                        id,
                        attributes {
                            id,
                            type,
                            ... on PostAttributePicture {
                                image {
                                    id,
                                    type,
                                    hasVideo
                                }
                            },
                            ... on PostAttributeEmbed {
                                value
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

public interface IPostClient
{
    Task<ApiClientResult<Post>> GetAsync(int numberId, CancellationToken cancellationToken = default);
    Task<ApiClientResult<Post>> GetAsync(string nodeId, CancellationToken cancellationToken = default);
}