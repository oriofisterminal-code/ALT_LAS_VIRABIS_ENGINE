"""
ALT_LAS Engine - Window Mode Entry Point
Launches the game in a real window with OpenGL rendering.
"""

import sys
import os

# Add project root to path
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from src.window.manager import WindowManager, WindowConfig, get_window_manager
from src.window.input import WindowInputHandler, InputKey, get_window_input_handler
from src.window.config import PRESETS
from src.render.adapter import RenderAdapter, get_render_adapter


class WindowGame:
    """
    Main game class for window mode.
    Uses real window with OpenGL context.
    """
    
    def __init__(self, preset: str = "default"):
        # Get preset config
        config = PRESETS.get(preset, PRESETS["default"])
        
        # Initialize window
        self.window = WindowManager(config)
        self.input_handler = WindowInputHandler()
        
        # Game state
        self.is_running = False
        self.scene = None
        self.fps = 60
        self.debug_mode = False
        
        # Timing
        import time
        self._last_time = time.time()
        self._delta_time = 0.0
    
    def initialize(self) -> bool:
        """Initialize window and rendering systems."""
        print("[WindowGame] Initializing...")
        
        # Initialize window
        if not self.window.initialize():
            print("[WindowGame] Failed to initialize window!")
            return False
        
        # Initialize input
        if not self.input_handler.initialize():
            print("[WindowGame] Warning: Input handler not fully initialized")
        
        # Initialize render adapter (optional, for GPU effects)
        try:
            adapter = get_render_adapter()
            adapter.initialize()
        except Exception as e:
            print(f"[WindowGame] Render adapter: {e}")
        
        self.is_running = True
        print(f"[WindowGame] Ready! Window: {self.window.width}x{self.window.height}")
        return True
    
    def run(self) -> None:
        """Main game loop."""
        if not self.initialize():
            return
        
        try:
            while self.is_running and not self.window.should_close:
                self._frame()
        except KeyboardInterrupt:
            pass
        finally:
            self.shutdown()
    
    def _frame(self) -> None:
        """Single frame processing."""
        import time
        
        # Calculate delta time
        now = time.time()
        self._delta_time = now - self._last_time
        self._last_time = now
        
        # Poll input
        self.input_handler.poll()
        
        # Handle input
        self._handle_input()
        
        # Update
        self._update()
        
        # Render
        self._render()
        
        # Swap buffers
        self.window.swap_buffers()
        
        # Frame rate limiting
        target_time = 1.0 / self.fps
        if self._delta_time < target_time:
            time.sleep(target_time - self._delta_time)
    
    def _handle_input(self) -> None:
        """Process input events."""
        # Close on Escape
        if self.input_handler.is_key_just_pressed(InputKey.ESCAPE):
            self.window.close()
            return
        
        # Debug toggle
        if self.input_handler.is_key_just_pressed(InputKey.F1):
            self.debug_mode = not self.debug_mode
        
        # Scene input
        if self.scene:
            for key in self.input_handler.get_pressed_keys():
                key_name = self.input_handler.get_legacy_key_name(key)
                self.scene.handle_input(key_name)
    
    def _update(self) -> None:
        """Update game state."""
        if self.scene:
            self.scene.update(self._delta_time)
    
    def _render(self) -> None:
        """Render frame."""
        # Clear screen
        self.window.clear(0.0, 0.0, 0.05, 1.0)
        
        # Get OpenGL context
        ctx = self.window.ctx
        
        if ctx and self.scene:
            # Render scene using OpenGL
            self.scene.render()
        
        # Debug overlay
        if self.debug_mode:
            self._render_debug()
    
    def _render_debug(self) -> None:
        """Render debug overlay."""
        # Would render FPS, etc.
        fps = int(1.0 / self._delta_time) if self._delta_time > 0 else 0
        self.window.set_title(f"ALT_LAS Engine - FPS: {fps}")
    
    def set_scene(self, scene) -> None:
        """Set the current scene."""
        self.scene = scene
    
    def shutdown(self) -> None:
        """Clean up and exit."""
        print("[WindowGame] Shutting down...")
        self.is_running = False
        self.input_handler.shutdown()
        self.window.shutdown()


def main():
    """Entry point for window mode."""
    import argparse
    
    parser = argparse.ArgumentParser(description="ALT_LAS Engine - Window Mode")
    parser.add_argument(
        "--preset", "-p",
        choices=list(PRESETS.keys()),
        default="default",
        help="Window preset configuration"
    )
    parser.add_argument(
        "--fullscreen", "-f",
        action="store_true",
        help="Start in fullscreen mode"
    )
    parser.add_argument(
        "--debug", "-d",
        action="store_true",
        help="Enable debug mode from start"
    )
    
    args = parser.parse_args()
    
    # Create game
    game = WindowGame(preset=args.preset)
    
    if args.debug:
        game.debug_mode = True
    
    if args.fullscreen:
        game.window.config.fullscreen = True
    
    # Run
    game.run()


if __name__ == "__main__":
    main()
