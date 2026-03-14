"""
ALT_LAS Engine - HTTP API Server
REST API for AI to control the engine via HTTP.
Runs in a background thread alongside the game engine.

Endpoints:
  POST /api/command   - Execute a single command
  POST /api/batch     - Execute multiple commands
  GET  /api/state     - Get engine status
  GET  /api/capabilities - List all tools (discover)
  GET  /api/health    - Health check
  OPTIONS /*          - CORS preflight
"""

import json
import time
import threading
from http.server import HTTPServer, BaseHTTPRequestHandler

from src.api.response import success, error
from src.api.router import CommandRouter
from src.api.middleware import (
    RateLimiter, AuthMiddleware, RequestLogger,
    apply_cors_headers, _load_api_config,
)
from src.core.command_queue import get_command_queue


class APIHandler(BaseHTTPRequestHandler):
    """HTTP request handler for the ALT_LAS API."""

    # Class-level references (set by APIServer)
    router: CommandRouter = None
    auth: AuthMiddleware = None
    rate_limiter: RateLimiter = None
    logger: RequestLogger = None
    cors_origins: list = None

    def log_message(self, format, *args):
        """Suppress default HTTP logging (we use our own logger)."""
        pass

    def _get_client_id(self) -> str:
        return self.client_address[0]

    def _read_body(self) -> dict:
        length = int(self.headers.get("Content-Length", 0))
        if length == 0:
            return {}
        raw = self.rfile.read(length)
        return json.loads(raw.decode("utf-8"))

    def _send_json(self, data: dict, status: int = 200) -> None:
        body = json.dumps(data, ensure_ascii=False, indent=2).encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        apply_cors_headers(self, self.cors_origins)
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def _check_auth(self) -> bool:
        if not self.auth or not self.auth.auth_required:
            return True
        key = self.headers.get("X-API-Key", "")
        if not key:
            auth_header = self.headers.get("Authorization", "")
            if auth_header.startswith("Bearer "):
                key = auth_header[7:]
        return self.auth.check(key)

    def _check_rate_limit(self) -> bool:
        if not self.rate_limiter:
            return True
        return self.rate_limiter.is_allowed(self._get_client_id())

    def do_OPTIONS(self):
        self.send_response(204)
        apply_cors_headers(self, self.cors_origins)
        self.end_headers()

    def do_GET(self):
        start = time.time()
        path = self.path.split("?")[0].rstrip("/")

        if not self._check_auth():
            self._send_json(error("UNAUTHORIZED", "Invalid or missing API key"), 401)
            return

        if not self._check_rate_limit():
            self._send_json(error("RATE_LIMITED", "Too many requests"), 429)
            return

        if path == "/api/health":
            self._send_json(success({
                "status": "healthy",
                "engine": "ALT_LAS",
                "api_version": "1.0.0",
                "uptime": time.time(),
            }, "OK"))

        elif path == "/api/state":
            result = self.router.execute("engine_status", {})
            self._send_json(result)

        elif path == "/api/capabilities":
            result = self.router.execute("discover", {})
            self._send_json(result)

        else:
            self._send_json(error("NOT_FOUND", f"Unknown endpoint: {path}"), 404)

        if self.logger:
            duration = (time.time() - start) * 1000
            self.logger.log("GET", path, 200, duration, self._get_client_id())

    def do_POST(self):
        start = time.time()
        path = self.path.split("?")[0].rstrip("/")

        if not self._check_auth():
            self._send_json(error("UNAUTHORIZED", "Invalid or missing API key"), 401)
            return

        if not self._check_rate_limit():
            self._send_json(error("RATE_LIMITED", "Too many requests"), 429)
            return

        try:
            body = self._read_body()
        except (json.JSONDecodeError, ValueError) as e:
            self._send_json(error("INVALID_JSON", f"Invalid JSON: {str(e)}"), 400)
            return

        if path == "/api/command":
            tool = body.get("tool")
            args = body.get("args", {})
            if not tool:
                self._send_json(error("MISSING_PARAM", "'tool' field required"), 400)
                return

            # Push to command queue for thread-safe execution
            queue = get_command_queue()
            cmd = queue.push(tool, args)
            result = cmd.wait(timeout=10.0)

            if result is None:
                # Fallback: execute directly if queue wasn't processed
                result = self.router.execute(tool, args)

            self._send_json(result)

        elif path == "/api/batch":
            commands = body.get("commands", [])
            if not commands:
                self._send_json(
                    error("MISSING_PARAM", "'commands' list required"), 400)
                return
            result = self.router.execute("batch_execute", {"commands": commands})
            self._send_json(result)

        else:
            self._send_json(error("NOT_FOUND", f"Unknown endpoint: {path}"), 404)

        if self.logger:
            duration = (time.time() - start) * 1000
            self.logger.log("POST", path, 200, duration, self._get_client_id())


class APIServer:
    """Manages the HTTP API server lifecycle."""

    def __init__(self, router: CommandRouter, host: str = "127.0.0.1",
                 port: int = 8080, api_key: str = "",
                 rate_limit: int = 60, rate_window: int = 60):
        self.router = router
        self.host = host
        self.port = port
        self.api_key = api_key

        # Configure handler class
        self.auth = AuthMiddleware(api_key)
        self.rate_limiter = RateLimiter(rate_limit, rate_window)
        self.logger = RequestLogger(enabled=True)

        self._server: HTTPServer = None
        self._thread: threading.Thread = None

    def start(self) -> None:
        """Start the API server in a background thread."""
        # Inject dependencies into handler class
        APIHandler.router = self.router
        APIHandler.auth = self.auth
        APIHandler.rate_limiter = self.rate_limiter
        APIHandler.logger = self.logger
        APIHandler.cors_origins = ["*"]

        self._server = HTTPServer((self.host, self.port), APIHandler)
        self._thread = threading.Thread(target=self._server.serve_forever,
                                        daemon=True)
        self._thread.start()
        print(f"[API] ALT_LAS API server started on http://{self.host}:{self.port}")
        if self.auth.auth_required:
            print(f"[API] Authentication: ENABLED (use X-API-Key header)")
        else:
            print(f"[API] Authentication: DISABLED (no API key configured)")

    def stop(self) -> None:
        """Stop the API server."""
        if self._server:
            self._server.shutdown()
            print("[API] API server stopped")

    @property
    def is_running(self) -> bool:
        return self._thread is not None and self._thread.is_alive()

    @classmethod
    def from_config(cls, router: CommandRouter) -> "APIServer":
        """Create an APIServer from the engine config file."""
        config = _load_api_config()
        return cls(
            router=router,
            host=config.get("host", "127.0.0.1"),
            port=config.get("port", 8080),
            api_key=config.get("api_key", ""),
            rate_limit=config.get("rate_limit", 60),
            rate_window=config.get("rate_window", 60),
        )
