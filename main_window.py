"""
ALT_LAS Engine - Window Mode Entry Point
Launches the game in a real window with OpenGL rendering.

Usage:
    python main_window.py              # Run graphics demo
    python main_window.py --demo       # Run graphics demo (explicit)
    python main_window.py --game       # Run full game (when ready)
    python main_window.py --fullscreen # Fullscreen mode
"""

import sys
import os

# Add project root to path
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from src.window.manager import WindowManager, WindowConfig, get_window_manager
from src.window.input import WindowInputHandler, InputKey, get_window_input_handler
from src.window.config import PRESETS


class WindowGame:
    """
    Main game class for window mode.
    Uses real window with OpenGL context.
    """

    def __init__(self, preset: str = "default", demo_mode: bool = True):
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
        self.demo_mode = demo_mode

        # Timing
        import time
        self._last_time = time.time()
        self._delta_time = 0.0
        self._frame_count = 0

        # Key tracking for demo
        self._keys_down = set()

    def initialize(self) -> bool:
        """Initialize window and rendering systems."""
        print("[WindowGame] Initializing...")
        print("=" * 50)

        # Initialize window
        if not self.window.initialize():
            print("[WindowGame] Failed to initialize window!")
            print("[WindowGame] Make sure you have moderngl-window installed:")
            print("  pip install moderngl-window")
            return False

        # Initialize input
        if not self.input_handler.initialize():
            print("[WindowGame] Warning: Input handler not fully initialized")

        # Load demo scene
        if self.demo_mode:
            self._load_demo_scene()

        self.is_running = True
        print("=" * 50)
        print(f"[WindowGame] Ready! Window: {self.window.width}x{self.window.height}")
        print("[WindowGame] Controls: WASD/Arrows = Move, ESC = Exit, F1 = Debug")
        print("=" * 50)
        return True

    def _load_demo_scene(self) -> None:
        """Load the graphics demo scene."""
        try:
            from src.game.demo_scene import GraphicsDemoScene

            ctx = self.window.ctx
            if ctx:
                self.scene = GraphicsDemoScene(ctx, self.window.width, self.window.height)
                if self.scene.initialize():
                    print("[WindowGame] Graphics Demo Scene loaded")
                    print("[WindowGame] Features: GPU Shaders, Lights, Particles, Sprites")
                else:
                    print("[WindowGame] Warning: Demo scene failed to initialize")
                    self.scene = None
        except ImportError as e:
            print(f"[WindowGame] Could not load demo scene: {e}")
            self.scene = None
        except Exception as e:
            print(f"[WindowGame] Demo scene error: {e}")
            self.scene = None

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
        self._frame_count += 1

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
            print(f"[WindowGame] Debug mode: {'ON' if self.debug_mode else 'OFF'}")
            if self.scene and hasattr(self.scene, '_show_debug_info'):
                self.scene._show_debug_info()

        # Get currently pressed keys
        pressed_keys = self.input_handler.get_pressed_keys()
        
        # Debug: show pressed keys
        if self.debug_mode and pressed_keys:
            key_names = [self.input_handler.get_legacy_key_name(k) for k in pressed_keys]
            # print(f"[Input] Pressed: {key_names}")

        # Track key states for scene
        for key in pressed_keys:
            key_name = self.input_handler.get_legacy_key_name(key)
            if key_name not in self._keys_down:
                self._keys_down.add(key_name)
                # print(f"[Input] New key down: {key_name}")
                if self.scene and hasattr(self.scene, 'handle_key'):
                    self.scene.handle_key(key_name.replace('TK_', ''), True)

        # Check for released keys
        current_key_names = {self.input_handler.get_legacy_key_name(k) for k in pressed_keys}
        released = self._keys_down - current_key_names
        for key_name in released:
            self._keys_down.discard(key_name)
            # print(f"[Input] Key released: {key_name}")
            if self.scene and hasattr(self.scene, 'handle_key'):
                self.scene.handle_key(key_name.replace('TK_', ''), False)

    def _update(self) -> None:
        """Update game state."""
        if self.scene:
            self.scene.update(self._delta_time)

    def _render(self) -> None:
        """Render frame."""
        # Clear screen
        self.window.clear(0.02, 0.02, 0.05, 1.0)

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
        fps = int(1.0 / self._delta_time) if self._delta_time > 0 else 0
        self.window.set_title(f"ALT_LAS Engine - FPS: {fps} | Frame: {self._frame_count}")

    def set_scene(self, scene) -> None:
        """Set the current scene."""
        self.scene = scene

    def shutdown(self) -> None:
        """Clean up and exit."""
        print("[WindowGame] Shutting down...")
        self.is_running = False

        if self.scene and hasattr(self.scene, 'shutdown'):
            self.scene.shutdown()

        self.input_handler.shutdown()
        self.window.shutdown()
        print("[WindowGame] Goodbye!")


def main():
    """Entry point for window mode."""
    import argparse

    parser = argparse.ArgumentParser(
        description="ALT_LAS Engine - Window Mode",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
    python main_window.py              # Run graphics demo
    python main_window.py --fullscreen # Fullscreen demo
    python main_window.py --debug      # Debug mode
        """
    )
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
    parser.add_argument(
        "--demo",
        action="store_true",
        default=True,
        help="Run graphics demo (default)"
    )
    parser.add_argument(
        "--game",
        action="store_true",
        help="Run full game mode (when available)"
    )

    args = parser.parse_args()

    # Create game
    demo_mode = not args.game  # Default to demo unless --game specified
    game = WindowGame(preset=args.preset, demo_mode=demo_mode)

    if args.debug:
        game.debug_mode = True

    if args.fullscreen:
        game.window.config.fullscreen = True

    # Run
    game.run()


if __name__ == "__main__":
    main()
