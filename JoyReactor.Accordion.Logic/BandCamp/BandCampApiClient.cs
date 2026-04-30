using JoyReactor.Accordion.Logic.BandCamp.Responses;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace JoyReactor.Accordion.Logic.BandCamp;

public partial class BandCampApiClient(HttpClient httpClient)
    : IBandCampApiClient
{
    [GeneratedRegex(@"<meta name=""bc-page-properties"" content=""([^""]+)"">")]
    private static partial Regex BcPagePropertiesRegex();

    public async Task<BandCampInfoResponse?> GetInfoAsync(string url, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync(cancellationToken);

        var bcPagePropertiesMatch = BcPagePropertiesRegex().Match(html);
        if (!bcPagePropertiesMatch.Success)
            return null;

        var bcPagePropertiesJson = WebUtility.HtmlDecode(bcPagePropertiesMatch.Groups[1].Value);
        return JsonSerializer.Deserialize<BandCampInfoResponse>(bcPagePropertiesJson) ?? throw new NullReferenceException();
    }
}

public interface IBandCampApiClient
{
    Task<BandCampInfoResponse?> GetInfoAsync(string url, CancellationToken cancellationToken);
}