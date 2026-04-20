using GraphQL;
using JoyReactor.Accordion.Logic.ApiClient.Models;
using JoyReactor.Accordion.Logic.ApiClient.Responses;
using JoyReactor.Accordion.Logic.Database.Sql.Entities;
using Microsoft.Extensions.Logging;

namespace JoyReactor.Accordion.Logic.ApiClient;

public class ChangedPostClient(
    IApiClientProvider apiClientProvider,
    ILogger<ChangedPostClient> logger)
    : IChangedPostClient
{
    public async Task<Post[]> GetAsync(Api api, DateOnly day, CancellationToken cancellationToken)
    {
        const string query = $$"""
query {{nameof(ChangedPostClient)}}_{{nameof(GetAsync)}}($day: Date!) {
  changedPosts(day: $day) {
    id
    contentVersion
    nsfw
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
        if (response == null)
        {
            logger.LogWarning("Changed posts response is empty for {Day}.", day);
            return [];
        }

        return response.ChangedPosts;
    }
}

public interface IChangedPostClient
{
    Task<Post[]> GetAsync(Api api, DateOnly day, CancellationToken cancellationToken);
}