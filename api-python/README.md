# sub-to-me API

FastAPI backend that handles device registration and sends Firebase push notifications.

## Prerequisites

- Python 3.10+
- A Firebase project with Cloud Messaging enabled
- A Firebase service account key (`serviceAccountKey.json`)

## Setup

### 1. Install dependencies

```bash
pip install -r requirements.txt
```

### 2. Configure environment variables

Copy the example env file and fill in your values:

```bash
cp .env.example .env
```

| Variable | Description |
|---|---|
| `APP_USERNAME` | Username for the login endpoint |
| `PASSWORD` | Password for the login endpoint |
| `JWT_SECRET` | A long random string used to sign JWT tokens |
| `SERVICE_ACCOUNT_PATH` | Path to your Firebase service account JSON file |

Generate a strong `JWT_SECRET` with:
```bash
python -c "import secrets; print(secrets.token_hex(32))"
```

### 3. Add your Firebase service account key

Download `serviceAccountKey.json` from the Firebase console:

> Firebase Console → Project Settings → Service Accounts → Generate new private key

Place the file in the api directory and set `SERVICE_ACCOUNT_PATH=./serviceAccountKey.json` in `.env`.

### 4. Run the server

```bash
uvicorn main:app --reload
```

The API will be available at `http://localhost:8000`.  
Interactive docs: `http://localhost:8000/docs`

## Endpoints

| Method | Path | Auth | Description |
|---|---|---|---|
| `POST` | `/login` | — | Returns a JWT Bearer token |
| `POST` | `/register-device` | — | Registers an FCM device token |
| `POST` | `/notify` | Bearer token | Sends a push notification to a registered device |

### POST `/login`
```json
{ "username": "admin", "password": "changeme" }
```
Returns `{ "access_token": "...", "token_type": "bearer" }`.

### POST `/register-device`
```json
{ "token": "<fcm-device-token>" }
```

### POST `/notify`
Requires `Authorization: Bearer <token>` header.
```json
{ "device_id": "<fcm-device-token>", "title": "Hello", "body": "World" }
```
