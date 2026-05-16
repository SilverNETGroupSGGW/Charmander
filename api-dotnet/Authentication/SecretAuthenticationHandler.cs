using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Charmander.Api.Authentication;

public sealed class SecretAuthenticationHandler : AuthenticationHandler<SecretAuthenticationOptions>
{
  private const string SchemePrefix = "Secret ";

  public SecretAuthenticationHandler(
      IOptionsMonitor<SecretAuthenticationOptions> options,
      ILoggerFactory logger,
      UrlEncoder encoder)
      : base(options, logger, encoder)
  {
  }

  protected override Task<AuthenticateResult> HandleAuthenticateAsync()
  {
    if (!Request.Headers.TryGetValue("Authorization", out var values))
      return Task.FromResult(AuthenticateResult.Fail("Missing Authorization header."));

    var header = values.ToString();
    if (!header.StartsWith(SchemePrefix, StringComparison.OrdinalIgnoreCase))
      return Task.FromResult(AuthenticateResult.Fail("Authorization scheme must be Secret."));

    var providedSecret = header[SchemePrefix.Length..];
    if (!FixedTimeEquals(providedSecret, Options.Secret))
      return Task.FromResult(AuthenticateResult.Fail("Invalid secret."));

    var identity = new ClaimsIdentity(
        [new Claim(ClaimTypes.Name, "api")],
        SecretAuthenticationDefaults.Scheme);
    var principal = new ClaimsPrincipal(identity);
    return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
  }

  protected override Task HandleChallengeAsync(AuthenticationProperties properties)
  {
    Response.Headers.WWWAuthenticate = "Secret";
    Response.StatusCode = StatusCodes.Status401Unauthorized;
    return Task.CompletedTask;
  }

  private static bool FixedTimeEquals(string a, string b)
  {
    var aBytes = Encoding.UTF8.GetBytes(a);
    var bBytes = Encoding.UTF8.GetBytes(b);
    return aBytes.Length == bBytes.Length
        && System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
  }
}
