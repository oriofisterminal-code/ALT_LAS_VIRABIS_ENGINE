"""ALT_LAS Engine - HTTP REST API Module for AI Integration.

This module provides a REST API server that allows external AI systems
to control the game engine via HTTP requests.

Endpoints:
  POST /api/command   - Execute a single command
  POST /api/batch     - Execute multiple commands
  GET  /api/state     - Get engine status
  GET  /api/capabilities - List all tools (discover)
  GET  /api/health    - Health check
"""

from src.api.server import APIServer, APIHandler
from src.api.router import CommandRouter, ALL_HANDLERS, TOOL_SCHEMAS, TOOL_CATEGORIES
from src.api.response import success, error
from src.api.middleware import RateLimiter, AuthMiddleware, RequestLogger

__all__ = [
    'APIServer', 'APIHandler',
    'CommandRouter', 'ALL_HANDLERS', 'TOOL_SCHEMAS', 'TOOL_CATEGORIES',
    'success', 'error',
    'RateLimiter', 'AuthMiddleware', 'RequestLogger',
]
