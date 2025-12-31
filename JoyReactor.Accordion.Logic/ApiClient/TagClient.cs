using GraphQL;
using GraphQL.Client.Abstractions;
using JoyReactor.Accordion.Logic.ApiClient.Models;
using System.Text;

namespace JoyReactor.Accordion.Logic.ApiClient;

public class TagClient(IGraphQLClient graphQlClient)
    : ITagClient
{
    public async Task<Tag> GetAsync(int numberId, CancellationToken cancellationToken = default)
    {
        const string query = @"
query TagClient_GetAsync($nodeId: ID!) {
  node(id: $nodeId) {
    ... on Tag {
      id
      name
      count
      subscribers
      mainTag {
        id
        name
      }
      hierarchy {
        id
        name
      }
      tagPager(type: NEW) {
        count
      }
    }
  }
}";

        var nodeId = Convert.ToBase64String(Encoding.UTF8.GetBytes($"Tag:{numberId}"));
        var request = new GraphQLRequest(query, new { nodeId });
        var response = await graphQlClient.SendQueryAsync<ApiClientResponse<Tag>>(request, cancellationToken);
        return response.Data.Node;
    }

    public async Task<Tag> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        const string query = @"
query TagClient_GetByNameAsync($name: String!) {
  node: tag(name: $name) {
    ... on Tag {
      id
      name
      count
      subscribers
      mainTag {
        id
        name
      }
      hierarchy {
        id
        name
      }
      tagPager(type: NEW) {
        count
      }
    }
  }
}";

        var request = new GraphQLRequest(query, new { name });
        var response = await graphQlClient.SendQueryAsync<ApiClientResponse<Tag>>(request, cancellationToken);
        return response.Data.Node;
    }



    public async Task<Tag[]> GetSubTagsAsync(int numberId, CancellationToken cancellationToken = default)
    {
        const string query = @"
query TagClient_GetSubTagsAsync($nodeId: ID!, $page: Int!) {
  node(id: $nodeId) {
    ... on Tag {
      tagPager(type: NEW) {
        tags(page: $page) {
          ... on Tag {
            id
            name
            count
            subscribers
            mainTag {
              id
              name
            }
            hierarchy {
              id
              name
            }
            tagPager(type: NEW) {
              count
            }
          }
        }
        count
      }
    }
  }
}";

        var nodeId = Convert.ToBase64String(Encoding.UTF8.GetBytes($"Tag:{numberId}"));
        var page = 0;
        GraphQLResponse<ApiClientResponse<Tag>> response = null;
        var subTags = new List<Tag>();

        do
        {
            page++;

            var request = new GraphQLRequest(query, new { nodeId, page });
            response = await graphQlClient.SendQueryAsync<ApiClientResponse<Tag>>(request, cancellationToken);
            subTags.AddRange(response.Data.Node.Pager.Tags);
        } while (subTags.Count() < response.Data.Node.Pager.SubTagsTotalCount);

        // Unknown behaivor
        // TagPager reports coult less then actual summary of pages
        // Example: VGFnOjEzMTU5Mjg=

        return subTags.ToArray();
    }
}

public interface ITagClient
{
    Task<Tag> GetAsync(int numberId, CancellationToken cancellationToken = default);
    Task<Tag> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<Tag[]> GetSubTagsAsync(int numberId, CancellationToken cancellationToken = default);
}