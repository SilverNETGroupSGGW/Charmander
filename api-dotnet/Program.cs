using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.Sqlite;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

DotNetEnv.Env.TraversePath().Load();

var apiSecret = RequireEnv("API_SECRET");
var jwtSecret = RequireEnv("JWT_SECRET");
var serviceAccountPath = RequireEnv("SERVICE_ACCOUNT_PATH");
const int accessTokenExpireMinutes = 60;
const string dbPath = "devices.db";

using var serviceAccountStream = File.OpenRead(serviceAccountPath);
var serviceAccount = CredentialFactory.FromStream<ServiceAccountCredential>(serviceAccountStream);
FirebaseApp.Create(new AppOptions
{
  Credential = serviceAccount.ToGoogleCredential(),
});

await InitDatabaseAsync();

var builder = WebApplication.CreateBuilder(args);

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
      options.TokenValidationParameters = new TokenValidationParameters
      {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = signingKey,
        ClockSkew = TimeSpan.Zero,
      };
    });
builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
  options.SwaggerDoc("v1", new OpenApiInfo
  {
    Title = "sub-to-me API",
    Version = "v1",
  });
  options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
  {
    Name = "Authorization",
    Type = SecuritySchemeType.Http,
    Scheme = "bearer",
    BearerFormat = "JWT",
    In = ParameterLocation.Header,
    Description = "JWT z POST /login { \"secret\": \"...\" } (przycisk Authorize)",
  });
  options.OperationFilter<AuthorizeOperationFilter>();
});

builder.Services.AddCors(options =>
{
  options.AddDefaultPolicy(policy =>
      policy.WithOrigins("http://localhost:5173")
          .AllowAnyMethod()
          .AllowAnyHeader());
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "sub-to-me API v1"));

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/login", (LoginRequest body) =>
{
  if (body.Secret != apiSecret)
  {
    return Results.Unauthorized();
  }

  var token = CreateAccessToken("api", jwtSecret, accessTokenExpireMinutes);
  return Results.Json(new TokenResponse(token, "bearer"));
})
.WithName("Login");

app.MapPost("/register-device", async (RegisterDeviceRequest body) =>
{
  await using var conn = OpenConnection();
  var insert = conn.CreateCommand();
  insert.CommandText = "INSERT OR IGNORE INTO device_tokens (token) VALUES ($token)";
  insert.Parameters.AddWithValue("$token", body.Token);
  await insert.ExecuteNonQueryAsync();

  var count = conn.CreateCommand();
  count.CommandText = "SELECT COUNT(*) FROM device_tokens";
  var total = (long)(await count.ExecuteScalarAsync() ?? 0L);

  Console.WriteLine($"Device registered: {body.Token}");
  return Results.Json(new { registered = body.Token, total }, statusCode: StatusCodes.Status201Created);
})
.WithName("RegisterDevice");

app.MapPost("/notify", async (NotifyRequest body) =>
{
  await using var conn = OpenConnection();
  var lookup = conn.CreateCommand();
  lookup.CommandText = "SELECT 1 FROM device_tokens WHERE token = $token";
  lookup.Parameters.AddWithValue("$token", body.Token);
  var exists = await lookup.ExecuteScalarAsync();
  if (exists is null)
  {
    return Results.NotFound(new { detail = "Device ID not registered" });
  }

  var message = new Message
  {
    Token = body.Token,
    Notification = new Notification
    {
      Title = body.Title,
      Body = body.Body,
    },
  };

  try
  {
    var messageId = await FirebaseMessaging.DefaultInstance.SendAsync(message);
    return Results.Json(new { message_id = messageId });
  }
  catch (Exception ex)
  {
    return Results.Json(
        new { detail = $"FCM error: {ex.Message}" },
        statusCode: StatusCodes.Status502BadGateway);
  }
})
.RequireAuthorization()
.WithName("Notify");

app.Run();

static string RequireEnv(string name) =>
    Environment.GetEnvironmentVariable(name)
    ?? throw new InvalidOperationException($"{name} is required");

static string CreateAccessToken(string subject, string secret, int expireMinutes)
{
  var credentials = new SigningCredentials(
      new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
      SecurityAlgorithms.HmacSha256);

  var token = new JwtSecurityToken(
      claims: [new Claim("sub", subject)],
      expires: DateTime.UtcNow.AddMinutes(expireMinutes),
      signingCredentials: credentials);

  return new JwtSecurityTokenHandler().WriteToken(token);
}

static async Task InitDatabaseAsync()
{
  await using var conn = OpenConnection();
  var cmd = conn.CreateCommand();
  cmd.CommandText = "CREATE TABLE IF NOT EXISTS device_tokens (token TEXT PRIMARY KEY)";
  await cmd.ExecuteNonQueryAsync();
}

static SqliteConnection OpenConnection()
{
  var conn = new SqliteConnection($"Data Source={dbPath}");
  conn.Open();
  return conn;
}

record TokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("token_type")] string TokenType);

record LoginRequest(string Secret);
record RegisterDeviceRequest(string Token);
record NotifyRequest(string Token, string Title, string Body);

sealed class AuthorizeOperationFilter : IOperationFilter
{
  public void Apply(OpenApiOperation operation, OperationFilterContext context)
  {
    if (!context.ApiDescription.ActionDescriptor.EndpointMetadata.OfType<IAuthorizeData>().Any())
      return;

    var scheme = new OpenApiSecuritySchemeReference("Bearer", context.Document);
    operation.Security ??= [];
    operation.Security.Add(new OpenApiSecurityRequirement { [scheme] = [] });
  }
}
