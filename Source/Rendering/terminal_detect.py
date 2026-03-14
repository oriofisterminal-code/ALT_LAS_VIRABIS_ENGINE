"""
ALT_LAS Engine - Terminal Capability Detection
Detects Sixel/Kitty support and terminal features.
Automatically selects best available rendering mode.
"""

import os
import sys
import subprocess
from typing import Optional
from enum import Enum


class RenderMode(Enum):
    """Available rendering modes."""
    SIXEL = "sixel"        # DEC Sixel protocol
    KITTY = "kitty"        # Kitty graphics protocol
    ITERM2 = "iterm2"      # iTerm2 inline images
    ASCII = "ascii"        # ANSI colored text fallback


class TerminalCapability:
    """Detects and stores terminal capabilities."""

    def __init__(self):
        self._term: Optional[str] = None
        self._color_depth: int = 0
        self._sixel_supported: bool = False
        self._kitty_supported: bool = False
        self._iterm2_supported: bool = False
        self._truecolor_supported: bool = False
        self._detected: bool = False
        self._render_mode: RenderMode = RenderMode.ASCII

    def detect(self) -> None:
        """Detect all terminal capabilities."""
        if self._detected:
            return

        self._term = os.environ.get("TERM", "")
        self._detect_color_support()
        self._detect_sixel_support()
        self._detect_kitty_support()
        self._detect_iterm2_support()
        self._select_render_mode()
        self._detected = True

    def _detect_color_support(self) -> None:
        """Detect color depth support (8, 256, or 24-bit)."""
        colorterm = os.environ.get("COLORTERM", "").lower()
        term = self._term.lower()

        if colorterm in ("truecolor", "24bit"):
            self._truecolor_supported = True
            self._color_depth = 24
        elif "256color" in term or "256color" in colorterm:
            self._color_depth = 8
        else:
            self._color_depth = 4

    def _detect_sixel_support(self) -> None:
        """Detect Sixel protocol support."""
        term = self._term.lower()
        term_program = os.environ.get("TERM_PROGRAM", "").lower()

        known_sixel_terms = {
            "xterm", "xterm-256color", "vt340", "vt382",
            "mlterm", "yaft-256color", "foot", "foot-extra",
            "st-256color", "st", "wezterm", "contour"
        }

        if term in known_sixel_terms:
            self._sixel_supported = self._query_sixel_capability()
        elif "wezterm" in term_program:
            self._sixel_supported = True
        elif term_program == "apple_terminal":
            self._sixel_supported = False
        else:
            self._sixel_supported = self._query_sixel_capability()

    def _query_sixel_capability(self) -> bool:
        """Query terminal for Sixel capability using DA1 request."""
        try:
            if not sys.stdout.isatty():
                return False
            old_settings = None
            try:
                import tty
                import termios
                fd = sys.stdin.fileno()
                old_settings = termios.tcgetattr(fd)
                tty.setraw(fd)
                sys.stdout.write("\x1b[c")
                sys.stdout.flush()
                import select
                if select.select([sys.stdin], [], [], 0.1)[0]:
                    response = sys.stdin.read(100)
                    if "4" in response or ";" in response:
                        if ";4" in response or "4;" in response:
                            return True
            finally:
                if old_settings:
                    termios.tcsetattr(fd, termios.TCSADRAIN, old_settings)
        except Exception:
            pass
        return False

    def _detect_kitty_support(self) -> None:
        """Detect Kitty graphics protocol support."""
        term_program = os.environ.get("TERM_PROGRAM", "")
        term = self._term.lower()

        if term_program == "kitty" or "kitty" in term:
            self._kitty_supported = True
            return

        try:
            if sys.stdout.isatty():
                sys.stdout.write("\x1b_Gi=31,a=q;\x1b\\")
                sys.stdout.flush()
        except Exception:
            pass
        self._kitty_supported = False

    def _detect_iterm2_support(self) -> None:
        """Detect iTerm2 inline image support."""
        term_program = os.environ.get("TERM_PROGRAM", "")
        if term_program == "iTerm.app":
            self._iterm2_supported = True
            return
        self._iterm2_supported = False

    def _select_render_mode(self) -> None:
        """Select the best available rendering mode."""
        if self._kitty_supported:
            self._render_mode = RenderMode.KITTY
        elif self._sixel_supported:
            self._render_mode = RenderMode.SIXEL
        elif self._iterm2_supported:
            self._render_mode = RenderMode.ITERM2
        else:
            self._render_mode = RenderMode.ASCII

    @property
    def render_mode(self) -> RenderMode:
        if not self._detected:
            self.detect()
        return self._render_mode

    @property
    def supports_sixel(self) -> bool:
        if not self._detected:
            self.detect()
        return self._sixel_supported

    @property
    def supports_kitty(self) -> bool:
        if not self._detected:
            self.detect()
        return self._kitty_supported

    @property
    def supports_truecolor(self) -> bool:
        if not self._detected:
            self.detect()
        return self._truecolor_supported

    @property
    def color_depth(self) -> int:
        if not self._detected:
            self.detect()
        return self._color_depth

    @property
    def is_interactive(self) -> bool:
        return sys.stdout.isatty()

    def get_terminal_size(self) -> tuple[int, int]:
        """Get terminal size in characters (width, height)."""
        try:
            size = os.get_terminal_size()
            return (size.columns, size.lines)
        except OSError:
            return (80, 24)


_terminal_capability: Optional[TerminalCapability] = None


def get_terminal_capability() -> TerminalCapability:
    """Get or create the global terminal capability instance."""
    global _terminal_capability
    if _terminal_capability is None:
        _terminal_capability = TerminalCapability()
        _terminal_capability.detect()
    return _terminal_capability


def get_render_mode() -> RenderMode:
    """Get the current rendering mode."""
    return get_terminal_capability().render_mode
