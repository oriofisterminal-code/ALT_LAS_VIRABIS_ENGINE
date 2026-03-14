"""
ALT_LAS Engine - Base Scene
Abstract base class for all game scenes.
"""

from typing import Optional


class BaseScene:
    """Interface that all scenes must implement."""

    def __init__(self):
        self.engine: Optional["GameEngine"] = None
        self.state_manager: Optional["StateManager"] = None

    def on_enter(self) -> None:
        """Called when scene becomes active."""
        pass

    def on_exit(self) -> None:
        """Called when scene is removed from stack."""
        pass

    def on_pause(self) -> None:
        """Called when another scene is pushed on top."""
        pass

    def on_resume(self) -> None:
        """Called when the scene on top is popped."""
        pass

    def handle_input(self, key: str) -> None:
        """Process input key."""
        pass

    def update(self, dt: float) -> None:
        """Update scene logic."""
        pass

    def render(self) -> None:
        """Draw the scene."""
        pass
