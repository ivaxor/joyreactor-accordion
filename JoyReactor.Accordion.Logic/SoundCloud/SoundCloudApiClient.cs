using JoyReactor.Accordion.Logic.SoundCloud.Responses;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace JoyReactor.Accordion.Logic.SoundCloud;

public partial class SoundCloudApiClient(
    HttpClient httpClient,
    IMemoryCache memoryCache,
    ILogger<SoundCloudApiClient> logger)
    : ISoundCloudApiClient
{
    [GeneratedRegex(@"src=""(https://a-v2\.sndcdn\.com/assets/[^""]+\.js)""")]
    private static partial Regex ScriptRegex();

    [GeneratedRegex(@"client_id[:=""]""([a-zA-Z0-9]{32})""")]
    private static partial Regex ClientIdRegex();

    public async Task<ISoundCloudResponse?> GetByPermaLinkAsync(string permaLinkUrl, CancellationToken cancellationToken)
    {
        var baseUrl = $"https://api-v2.soundcloud.com/resolve?url={permaLinkUrl}";
        return await GetAsync(baseUrl, cancellationToken);
    }

    public async Task<ISoundCloudResponse?> GetByIdAsync(string urlPart, CancellationToken cancellationToken)
    {
        var baseUrl = $"https://api-v2.soundcloud.com/{urlPart}";
        return await GetAsync(baseUrl, cancellationToken);
    }

    protected async Task<string> GetClientId(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://soundcloud.com");
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync(cancellationToken);

        var scriptMatches = ScriptRegex().Matches(html);
        var scriptUrls = scriptMatches.Select(m => m.Groups[1].Value).Reverse();

        foreach (var scriptUrl in scriptUrls)
        {
            try
            {
                using var scriptRequest = new HttpRequestMessage(HttpMethod.Get, scriptUrl);
                using var scriptResponse = await httpClient.SendAsync(scriptRequest, cancellationToken);
                response.EnsureSuccessStatusCode();

                var script = await scriptResponse.Content.ReadAsStringAsync(cancellationToken);
                var clientIdMatches = ClientIdRegex().Match(script);

                if (clientIdMatches.Success)
                    return clientIdMatches.Groups[1].Value;
            }
            catch (Exception) { }
        }

        throw new Exception("Failed to find client_id in scripts.");
    }

    protected async Task<ISoundCloudResponse?> GetAsync(string baseUrl, CancellationToken cancellationToken)
    {
        const string memoryCacheKey = "SoundCloud_ClientId";
        var clientId = await memoryCache.GetOrCreateAsync(memoryCacheKey, async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromDays(1);
            return await GetClientId(cancellationToken);
        });

        var url = QueryHelpers.AddQueryString(baseUrl, "client_id", clientId);
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        switch (response.StatusCode)
        {
            case HttpStatusCode.OK:
                return await response.Content.ReadFromJsonAsync<SoundCloudBaseResponse>(cancellationToken);

            case HttpStatusCode.NotFound:
                return null;

            case HttpStatusCode.Unauthorized:
                logger.LogWarning("SoundCloud API ClientId expired.");
                memoryCache.Remove(memoryCacheKey);
                break;
        }

        response.EnsureSuccessStatusCode();
        return null;
    }
}

public interface ISoundCloudApiClient
{
    Task<ISoundCloudResponse?> GetByPermaLinkAsync(string permaLinkUrl, CancellationToken cancellationToken);
    Task<ISoundCloudResponse?> GetByIdAsync(string urlPart, CancellationToken cancellationToken);
}