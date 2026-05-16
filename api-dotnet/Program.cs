using Charmander.Api;
using Charmander.Api.Authentication;
using Charmander.Api.Models;
using Charmander.Api.Repositories;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

DotNetEnv.Env.TraversePath().Load();

var apiSecret = RequireEnv("API_SECRET");
var serviceAccountPath = RequireEnv("SERVICE_ACCOUNT_PATH");

using var serviceAccountStream = File.OpenRead(serviceAccountPath);
var serviceAccount = CredentialFactory.FromStream<ServiceAccountCredential>(serviceAccountStream);
FirebaseApp.Create(new AppOptions
{
  Credential = serviceAccount.ToGoogleCredential(),
});

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCharmanderAuth(apiSecret);
builder.Services.AddCharmanderSwagger();
var dbConnection = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING")
    ?? "Data Source=devices.db";
builder.Services.AddDeviceTokenRepository(dbConnection);

builder.Services.AddCors(options =>
{
  options.AddDefaultPolicy(policy =>
      policy.WithOrigins("http://localhost:5173")
          .AllowAnyMethod()
          .AllowAnyHeader());
});

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
  var repository = scope.ServiceProvider.GetRequiredService<IDeviceTokenRepository>();
  await repository.InitializeAsync();
}

app.UseCharmanderSwagger();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/register-device", async (RegisterDeviceRequest body, IDeviceTokenRepository repository) =>
{
  var total = await repository.RegisterAsync(body.Token);

  Console.WriteLine($"Device registered: {body.Token}");
  return Results.Json(new { registered = body.Token, total }, statusCode: StatusCodes.Status201Created);
})
.RequireSecret()
.WithName("RegisterDevice")
.WithSummary("Zapisuje token FCM urządzenia w bazie SQLite.");

app.MapPost("/notify", async (NotifyRequest body, IDeviceTokenRepository repository) =>
{
  if (!await repository.ExistsAsync(body.Token))
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
.RequireSecret()
.WithName("Notify")
.WithSummary("Wysyła powiadomienie push przez Firebase Cloud Messaging do wcześniej zarejestrowanego tokenu.");

app.Run();

static string RequireEnv(string name)
{
  return Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException($"{name} is required");
}
