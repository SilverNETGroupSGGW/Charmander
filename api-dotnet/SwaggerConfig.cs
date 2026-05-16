using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Charmander.Api;

public static class SwaggerConfig
{
  public static IServiceCollection AddCharmanderSwagger(this IServiceCollection services)
  {
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen(options =>
    {
      options.SwaggerDoc("v1", new OpenApiInfo
      {
        Title = "Charmander API",
        Version = "v1",
        Description =
            "API do rejestracji tokenów FCM i wysyłania powiadomień push. " +
            "Chronione endpointy wymagają nagłówka `Authorization: Secret <API_SECRET>`.",
      });
      options.AddSecurityDefinition("Secret", new OpenApiSecurityScheme
      {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "Authorization: Secret <API_SECRET>",
      });
      options.OperationFilter<AuthorizeOperationFilter>();
      options.OperationFilter<EndpointMetadataOperationFilter>();

      var xmlPath = Path.Combine(
          AppContext.BaseDirectory,
          $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
      if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
    });
    return services;
  }

  public static WebApplication UseCharmanderSwagger(this WebApplication app)
  {
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Charmander API v1"));
    return app;
  }
}

sealed class AuthorizeOperationFilter : IOperationFilter
{
  public void Apply(OpenApiOperation operation, OperationFilterContext context)
  {
    if (!context.ApiDescription.ActionDescriptor.EndpointMetadata.OfType<IAuthorizeData>().Any())
      return;

    var scheme = new OpenApiSecuritySchemeReference("Secret", context.Document);
    operation.Security ??= [];
    operation.Security.Add(new OpenApiSecurityRequirement { [scheme] = [] });
  }
}

sealed class EndpointMetadataOperationFilter : IOperationFilter
{
  public void Apply(OpenApiOperation operation, OperationFilterContext context)
  {
    var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;

    var summary = metadata.OfType<IEndpointSummaryMetadata>().LastOrDefault()?.Summary;
    if (!string.IsNullOrWhiteSpace(summary))
      operation.Summary = summary;

    var description = metadata.OfType<IEndpointDescriptionMetadata>().LastOrDefault()?.Description;
    if (!string.IsNullOrWhiteSpace(description))
      operation.Description = description;
  }
}
