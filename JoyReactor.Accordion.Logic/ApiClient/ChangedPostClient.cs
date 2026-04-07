using GraphQL;
using JoyReactor.Accordion.Logic.ApiClient.Models;
using JoyReactor.Accordion.Logic.ApiClient.Responses;
using JoyReactor.Accordion.Logic.Database.Sql.Entities;

namespace JoyReactor.Accordion.Logic.ApiClient;

public class ChangedPostClient(IApiClientProvider apiClientProvider)
    : IChangedPostClient
{
    public async Task<Post[]> GetAsync(Api api, DateOnly day, CancellationToken cancellationToken)
    {
        const string query = $$"""
query {{nameof(ChangedPostClient)}}_{{nameof(GetAsync)}}($day: Date!) {
  changedPosts(day: $day) {
    id
    contentVersion
    attributes {
      type
      ... on PostAttributePicture {
        id
        image {
          id
          type
        }
      }
      ... on PostAttributeEmbed {
        id
        value
      }
    }
  }
}
""";

        var apiClient = apiClientProvider.Provide(api);

        var request = new GraphQLRequest(query, new { day });
        var response = await apiClient.SendAsync<ChangedPostsResponse>(request, cancellationToken);
        return response.ChangedPosts;
    }
}

public interface IChangedPostClient
{
    Task<Post[]> GetAsync(Api api, DateOnly day, CancellationToken cancellationToken);
}