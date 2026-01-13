using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace JoyReactor.Accordion.WebAPI.Auth;

public class ApiKeyAuthenticationSchemeHandler(
    IOptionsMonitor<ApiKeyAuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<ApiKeyAuthenticationSchemeOptions>(options, logger, encoder)
{
    private const string HeaderName = "X-API-Key";
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var apiKey = Context.Request.Headers[HeaderName];
        var isApiKeyValid = Options.ApiKeys.Contains(apiKey);
        if (!isApiKeyValid)
            return Task.FromResult(AuthenticateResult.Fail($"Invalid {HeaderName} value"));

        var identity = new ClaimsIdentity(Enumerable.Empty<Claim>(), Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}