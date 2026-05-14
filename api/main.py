import os
import sqlite3
from datetime import datetime, timedelta, timezone

import firebase_admin
from firebase_admin import credentials, messaging
from dotenv import load_dotenv
from fastapi import Depends, FastAPI, HTTPException, status
from fastapi.security import OAuth2PasswordBearer, OAuth2PasswordRequestForm
from jose import JWTError, jwt
from pydantic import BaseModel

# ---------------------------------------------------------------------------
# Config
# ---------------------------------------------------------------------------

load_dotenv()

USERNAME = os.getenv("APP_USERNAME")
PASSWORD = os.getenv("PASSWORD")
JWT_SECRET = os.getenv("JWT_SECRET")
ALGORITHM = "HS256"
ACCESS_TOKEN_EXPIRE_MINUTES = 60
SERVICE_ACCOUNT_PATH = os.getenv("SERVICE_ACCOUNT_PATH")

firebase_app = firebase_admin.initialize_app(
    credentials.Certificate(SERVICE_ACCOUNT_PATH)
)

# ---------------------------------------------------------------------------
# Models
# ---------------------------------------------------------------------------


class Token(BaseModel):
    access_token: str
    token_type: str


class RegisterDeviceRequest(BaseModel):
    device_id: str


class NotifyRequest(BaseModel):
    device_id: str
    title: str
    body: str


# ---------------------------------------------------------------------------
# Auth utilities
# ---------------------------------------------------------------------------

oauth2_scheme = OAuth2PasswordBearer(tokenUrl="login")


def create_access_token(data: dict) -> str:
    payload = data.copy()
    payload["exp"] = datetime.now(timezone.utc) + timedelta(minutes=ACCESS_TOKEN_EXPIRE_MINUTES)
    return jwt.encode(payload, JWT_SECRET, algorithm=ALGORITHM)


def get_current_user(token: str = Depends(oauth2_scheme)) -> str:
    credentials_exception = HTTPException(
        status_code=status.HTTP_401_UNAUTHORIZED,
        detail="Invalid or expired token",
        headers={"WWW-Authenticate": "Bearer"},
    )
    try:
        payload = jwt.decode(token, JWT_SECRET, algorithms=[ALGORITHM])
        username: str = payload.get("sub")
        if username is None:
            raise credentials_exception
    except JWTError:
        raise credentials_exception
    return username


# ---------------------------------------------------------------------------
# Storage
# ---------------------------------------------------------------------------

DB_PATH = "devices.db"


def _get_db() -> sqlite3.Connection:
    conn = sqlite3.connect(DB_PATH)
    conn.execute(
        "CREATE TABLE IF NOT EXISTS device_tokens (token TEXT PRIMARY KEY)"
    )
    conn.commit()
    return conn

# ---------------------------------------------------------------------------
# App
# ---------------------------------------------------------------------------

app = FastAPI()


@app.post("/login", response_model=Token)
def login(form_data: OAuth2PasswordRequestForm = Depends()):
    if form_data.username != USERNAME or form_data.password != PASSWORD:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Incorrect username or password",
        )
    token = create_access_token({"sub": form_data.username})
    return Token(access_token=token, token_type="bearer")


@app.post("/register-device", status_code=status.HTTP_201_CREATED)
def register_device(request: RegisterDeviceRequest):
    with _get_db() as conn:
        conn.execute(
            "INSERT OR IGNORE INTO device_tokens (token) VALUES (?)",
            (request.device_id,),
        )
        conn.commit()
        total = conn.execute("SELECT COUNT(*) FROM device_tokens").fetchone()[0]
    return {"registered": request.device_id, "total": total}


@app.post("/notify", status_code=status.HTTP_200_OK)
def send_notification(
    request: NotifyRequest,
    _: str = Depends(get_current_user),
):
    with _get_db() as conn:
        row = conn.execute(
            "SELECT 1 FROM device_tokens WHERE token = ?", (request.device_id,)
        ).fetchone()
    if row is None:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="Device ID not registered",
        )
    message = messaging.Message(
        notification=messaging.Notification(
            title=request.title,
            body=request.body,
        ),
        token=request.device_id,
    )
    try:
        message_id = messaging.send(message)
    except Exception as e:
        raise HTTPException(
            status_code=status.HTTP_502_BAD_GATEWAY,
            detail=f"FCM error: {e}",
        )
    return {"message_id": message_id}
