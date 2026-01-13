using Microsoft.AspNetCore.Authentication;
using System.Collections.Frozen;

namespace JoyReactor.Accordion.WebAPI.Auth;

public class ApiKeyAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
    public FrozenSet<string> ApiKeys { get; set; }
}