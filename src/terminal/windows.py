"""
ALT_LAS Engine - Windows Terminal Setup
Enables Virtual Terminal Processing for ANSI escape codes on Windows.
"""

import sys
import os

if sys.platform == 'win32':
    import ctypes
    from ctypes import wintypes

    # Windows API constants
    ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004
    ENABLE_PROCESSED_OUTPUT = 0x0001
    DISABLE_NEWLINE_AUTO_RETURN = 0x0008
    
    STD_OUTPUT_HANDLE = -11
    STD_INPUT_HANDLE = -10

    kernel32 = ctypes.windll.kernel32


def enable_vt_mode() -> bool:
    """Enable Virtual Terminal mode on Windows for ANSI escape codes."""
    if sys.platform != 'win32':
        return True  # Not needed on Unix

    try:
        # Get stdout handle
        handle = kernel32.GetStdHandle(STD_OUTPUT_HANDLE)
        if handle == -1:
            return False

        # Get current mode
        mode = wintypes.DWORD()
        if not kernel32.GetConsoleMode(handle, ctypes.byref(mode)):
            return False

        # Enable VT processing
        new_mode = mode.value | ENABLE_VIRTUAL_TERMINAL_PROCESSING | ENABLE_PROCESSED_OUTPUT
        if not kernel32.SetConsoleMode(handle, new_mode):
            return False

        return True
    except Exception:
        return False


def enable_raw_input() -> bool:
    """Enable raw input mode on Windows."""
    if sys.platform != 'win32':
        return True  # Handled by termios on Unix

    try:
        handle = kernel32.GetStdHandle(STD_INPUT_HANDLE)
        if handle == -1:
            return False

        # Get current mode
        mode = wintypes.DWORD()
        if not kernel32.GetConsoleMode(handle, ctypes.byref(mode)):
            return False

        # Disable line input and echo for raw mode
        ENABLE_LINE_INPUT = 0x0002
        ENABLE_ECHO_INPUT = 0x0004
        ENABLE_PROCESSED_INPUT = 0x0001
        
        new_mode = mode.value & ~(ENABLE_LINE_INPUT | ENABLE_ECHO_INPUT)
        # Keep processed input for Ctrl+C to work
        new_mode |= ENABLE_PROCESSED_INPUT
        
        if not kernel32.SetConsoleMode(handle, new_mode):
            return False

        return True
    except Exception:
        return False


def restore_input_mode() -> bool:
    """Restore normal input mode on Windows."""
    if sys.platform != 'win32':
        return True

    try:
        handle = kernel32.GetStdHandle(STD_INPUT_HANDLE)
        if handle == -1:
            return False

        # Restore default mode
        default_mode = 0x0007  # ENABLE_PROCESSED_INPUT | ENABLE_LINE_INPUT | ENABLE_ECHO_INPUT
        kernel32.SetConsoleMode(handle, default_mode)
        return True
    except Exception:
        return False


def clear_screen() -> None:
    """Clear the terminal screen properly."""
    if sys.platform == 'win32':
        # Use Windows API for reliable clearing
        try:
            handle = kernel32.GetStdHandle(STD_OUTPUT_HANDLE)
            # Get console screen buffer info
            class CONSOLE_SCREEN_BUFFER_INFO(ctypes.Structure):
                _fields_ = [
                    ("dwSize", wintypes._COORD),
                    ("dwCursorPosition", wintypes._COORD),
                    ("wAttributes", wintypes.WORD),
                    ("srWindow", wintypes.SMALL_RECT),
                    ("dwMaximumWindowSize", wintypes._COORD),
                ]
            
            info = CONSOLE_SCREEN_BUFFER_INFO()
            kernel32.GetConsoleScreenBufferInfo(handle, ctypes.byref(info))
            
            # Calculate cells to fill
            cells = info.dwSize.X * info.dwSize.Y
            
            # Fill with spaces
            written = wintypes.DWORD()
            kernel32.FillConsoleOutputCharacterW(
                handle, ' ', cells, wintypes._COORD(0, 0), ctypes.byref(written)
            )
            
            # Reset attributes
            kernel32.FillConsoleOutputAttribute(
                handle, 0x0007, cells, wintypes._COORD(0, 0), ctypes.byref(written)
            )
            
            # Move cursor to top
            kernel32.SetConsoleCursorPosition(handle, wintypes._COORD(0, 0))
            return
        except Exception:
            pass
    
    # Fallback to ANSI
    sys.stdout.write("\x1b[2J\x1b[H")
    sys.stdout.flush()


# Initialize VT mode on import
_vt_enabled = False

def init_windows_terminal() -> bool:
    """Initialize Windows terminal for proper ANSI and input support."""
    global _vt_enabled
    
    if sys.platform != 'win32':
        return True
    
    _vt_enabled = enable_vt_mode()
    return _vt_enabled


def is_vt_enabled() -> bool:
    """Check if VT mode is enabled."""
    return _vt_enabled
