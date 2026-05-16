namespace Charmander.Api.Models;

/// <summary>Żądanie rejestracji tokenu FCM urządzenia.</summary>
/// <param name="Token">Token FCM z przeglądarki.</param>
public record RegisterDeviceRequest(string Token);
