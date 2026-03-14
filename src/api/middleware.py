"""
ALT_LAS Engine - API Middleware
Authentication, rate limiting, and request logging.
"""

import json
import os
import time
import hashlib
import secrets
from http.server import BaseHTTPRequestHandler

BASE_DIR = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))


def _load_api_config() -> dict:
    """Load API config from settings.json or use defaults."""
    config_path = os.path.join(BASE_DIR, "config", "settings.json")
    defaults = {
        "enabled": True,
        "port": 8080,
        "host": "127.0.0.1",
        "api_key": "",
        "rate_limit": 60,
        "rate_window": 60,
        "cors_origins": ["*"],
    }
    try:
        with open(config_path, "r", encoding="utf-8") as f:
            config = json.load(f)
        api_conf = config.get("api", {})
        for k, v in defaults.items():
            if k not in api_conf:
                api_conf[k] = v
        return api_conf
    except Exception:
        return defaults


class RateLimiter:
    """Simple in-memory rate limiter using sliding window."""

    def __init__(self, max_requests: int = 60, window_seconds: int = 60):
        self.max_requests = max_requests
        self.window = window_seconds
        self._requests: dict[str, list[float]] = {}

    def is_allowed(self, client_id: str) -> bool:
        now = time.time()
        if client_id not in self._requests:
            self._requests[client_id] = []

        # Clean old entries
        self._requests[client_id] = [
            t for t in self._requests[client_id] if now - t < self.window
        ]

        if len(self._requests[client_id]) >= self.max_requests:
            return False

        self._requests[client_id].append(now)
        return True

    def remaining(self, client_id: str) -> int:
        now = time.time()
        if client_id not in self._requests:
            return self.max_requests
        active = [t for t in self._requests[client_id] if now - t < self.window]
        return max(0, self.max_requests - len(active))


class AuthMiddleware:
    """API key authentication."""

    def __init__(self, api_key: str = ""):
        self.api_key = api_key
        if not self.api_key:
            self.api_key = os.environ.get("ALT_LAS_API_KEY", "")

    @property
    def auth_required(self) -> bool:
        return bool(self.api_key)

    def check(self, request_key: str) -> bool:
        if not self.auth_required:
            return True
        return secrets.compare_digest(request_key, self.api_key)


class RequestLogger:
    """Simple request logger."""

    def __init__(self, enabled: bool = True):
        self.enabled = enabled

    def log(self, method: str, path: str, status: int, duration_ms: float,
            client: str = "") -> None:
        if not self.enabled:
            return
        print(f"[API] {method} {path} -> {status} ({duration_ms:.1f}ms) [{client}]")


def generate_api_key() -> str:
    """Generate a random API key."""
    return secrets.token_urlsafe(32)


def apply_cors_headers(handler: BaseHTTPRequestHandler,
                       origins: list[str] = None) -> None:
    """Add CORS headers to a response."""
    if origins is None:
        origins = ["*"]
    origin = origins[0] if origins else "*"
    handler.send_header("Access-Control-Allow-Origin", origin)
    handler.send_header("Access-Control-Allow-Methods", "GET, POST, OPTIONS")
    handler.send_header("Access-Control-Allow-Headers",
                        "Content-Type, Authorization, X-API-Key")
    handler.send_header("Access-Control-Max-Age", "86400")
