using Microsoft.AspNetCore.Authentication;

namespace JoyReactor.Accordion.WebAPI.Auth;

public class ApiKeyAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
    public string[] ApiKeys { get; set; }
}