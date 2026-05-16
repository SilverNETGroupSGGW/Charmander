using Microsoft.AspNetCore.Authentication;

namespace Charmander.Api.Authentication;

public sealed class SecretAuthenticationOptions : AuthenticationSchemeOptions
{
  public string Secret { get; set; } = string.Empty;
}
