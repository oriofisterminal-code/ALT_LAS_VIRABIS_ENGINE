"""
ALT_LAS Engine - Standard API Response Format
All API responses follow this consistent structure.
"""


def success(data: dict = None, message: str = "OK") -> dict:
    """Create a successful response."""
    return {
        "success": True,
        "data": data or {},
        "message": message,
        "error": None,
    }


def error(code: str, detail: str, message: str = "Error") -> dict:
    """Create an error response."""
    return {
        "success": False,
        "data": None,
        "message": message,
        "error": {"code": code, "detail": detail},
    }
