# Charmander

API proxy do **subskrypcji powiadomień push w przeglądarce** (Firebase Cloud Messaging) oraz **wysyłania powiadomień** na zarejestrowane urządzenia przez API.

![Charmander](./img/charmander.jpg)

## Do czego służy

Użytkownik odwiedza stronę webową, wyraża zgodę na powiadomienia i rejestruje token FCM w backendzie. Administrator (lub inny system) może później wywołać API i wysłać push z tytułem i treścią na wybrane urządzenie.

Typowy przepływ:

1. **Frontend** (`www`) — użytkownik klika „Subscribe”, przeglądarka pobiera token FCM i wysyła go na `POST /register-device`.
2. **Backend** (`api` lub `api2`) — zapisuje token w SQLite i obsługuje wysyłkę przez Firebase Admin SDK.
3. **Wysyłka** — po zalogowaniu (`POST /login` z `API_SECRET`) wywołujesz `POST /notify` z tokenem urządzenia, tytułem i treścią.

## Struktura repozytorium

| Katalog | Opis |
|---------|------|
| [`www/`](www/) | Aplikacja React (Vite) — strona subskrypcji push |
| [`api/`](api/) | Backend w Pythonie (FastAPI) |
| [`api2/`](api2/) | Ten sam kontrakt API w .NET (Minimal API + Swagger) |

Oba backendy udostępniają te same endpointy:

| Metoda | Ścieżka | Auth | Opis |
|--------|---------|------|------|
| `POST` | `/login` | — | Wymiana `API_SECRET` na token JWT |
| `POST` | `/register-device` | — | Rejestracja tokenu FCM |
| `POST` | `/notify` | Bearer JWT | Wysłanie powiadomienia push |

## Wymagania

- Projekt [Firebase](https://console.firebase.google.com/) z włączonym **Cloud Messaging**
- Klucz konta serwisowego Firebase (`serviceAccountKey.json`) — do backendu
- Klucz VAPID (Web Push) — do frontendu

## Szybki start

Szczegóły konfiguracji i uruchomienia:

- Frontend: [`www/README.md`](www/README.md)
- API Python: [`api/README.md`](api/README.md)
- API .NET: uruchom `dotnet run` w katalogu `api2` (Swagger: `/swagger`)

W każdym backendzie skopiuj `.env.example` → `.env` i uzupełnij `API_SECRET`, `JWT_SECRET` oraz ścieżkę do klucza Firebase. W `www` ustaw `VITE_BASE_API` na adres działającego API.
