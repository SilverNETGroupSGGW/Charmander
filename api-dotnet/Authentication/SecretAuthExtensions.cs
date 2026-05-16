using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace Charmander.Api.Authentication;

public static class SecretAuthExtensions
{
  public static IServiceCollection AddCharmanderAuth(this IServiceCollection services, string secret)
  {
    services
        .AddAuthentication(options =>
        {
          options.DefaultAuthenticateScheme = SecretAuthenticationDefaults.Scheme;
          options.DefaultChallengeScheme = SecretAuthenticationDefaults.Scheme;
          options.DefaultForbidScheme = SecretAuthenticationDefaults.Scheme;
        })
        .AddScheme<SecretAuthenticationOptions, SecretAuthenticationHandler>(
            SecretAuthenticationDefaults.Scheme,
            options => options.Secret = secret);

    services.AddAuthorization(options =>
    {
      options.AddPolicy(SecretAuthenticationDefaults.Policy, policy =>
          policy
              .AddAuthenticationSchemes(SecretAuthenticationDefaults.Scheme)
              .RequireAuthenticatedUser());
    });

    return services;
  }

  public static RouteHandlerBuilder RequireSecret(this RouteHandlerBuilder builder) =>
      builder.RequireAuthorization(SecretAuthenticationDefaults.Policy);
}
