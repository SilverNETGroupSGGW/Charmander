namespace Charmander.Api.Models;

/// <summary>Żądanie wysłania powiadomienia push.</summary>
/// <param name="Token">Token FCM zarejestrowanego urządzenia.</param>
/// <param name="Title">Tytuł powiadomienia.</param>
/// <param name="Body">Treść powiadomienia.</param>
public record NotifyRequest(string Token, string Title, string Body);
