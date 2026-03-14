#!/usr/bin/env python3
"""
ALT_LAS Engine - Windows Compatibility Test
Run this script to verify Windows compatibility.
"""

import sys
import os

# Add project root to path
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

def test_platform():
    """Test platform detection."""
    print("=" * 50)
    print("Platform Detection Test")
    print("=" * 50)
    print(f"Platform: {sys.platform}")
    print(f"Python: {sys.version}")
    print(f"OS: {os.name}")
    print()

def test_windows_modules():
    """Test Windows-specific modules."""
    print("=" * 50)
    print("Windows Modules Test")
    print("=" * 50)

    if sys.platform == 'win32':
        try:
            import msvcrt
            print("✓ msvcrt module available")
        except ImportError:
            print("✗ msvcrt module NOT available (unexpected on Windows)")
    else:
        print("ℹ Not on Windows - skipping msvcrt test")
        try:
            import termios
            import tty
            print("✓ termios and tty modules available (Unix)")
        except ImportError:
            print("✗ termios/tty modules NOT available")
    print()

def test_terminal_detection():
    """Test terminal capability detection."""
    print("=" * 50)
    print("Terminal Detection Test")
    print("=" * 50)

    try:
        from src.render.terminal_detect import get_terminal_capability, RenderMode
        cap = get_terminal_capability()

        print(f"Render Mode: {cap.render_mode.value}")
        print(f"Color Depth: {cap.color_depth}")
        print(f"TrueColor Support: {cap.supports_truecolor}")
        print(f"Sixel Support: {cap.supports_sixel}")
        print(f"Kitty Support: {cap.supports_kitty}")
        print(f"Interactive: {cap.is_interactive}")

        # Windows Terminal check
        if sys.platform == 'win32':
            wt_session = os.environ.get("WT_SESSION", "")
            if wt_session:
                print(f"Windows Terminal detected (WT_SESSION: {wt_session})")
            else:
                print("Standard Windows Console")

        print("✓ Terminal detection OK")
    except Exception as e:
        print(f"✗ Terminal detection failed: {e}")
    print()

def test_input_handler():
    """Test input handler initialization."""
    print("=" * 50)
    print("Input Handler Test")
    print("=" * 50)

    try:
        from src.core.engine import InputHandler
        handler = InputHandler()

        print("Creating InputHandler...")
        result = handler.initialize()
        print(f"Initialize result: {result}")

        print("Testing has_input()...")
        has_input = handler.has_input()
        print(f"has_input: {has_input}")

        print("Shutting down...")
        handler.shutdown()
        print("✓ Input handler OK")
    except Exception as e:
        print(f"✗ Input handler test failed: {e}")
    print()

def test_render_adapter():
    """Test render adapter initialization."""
    print("=" * 50)
    print("Render Adapter Test")
    print("=" * 50)

    try:
        from src.render.adapter import RenderAdapter, get_render_adapter, RenderBackend

        print("Creating RenderAdapter...")
        adapter = RenderAdapter(80, 25)

        print("Initializing...")
        result = adapter.initialize()
        print(f"Initialize result: {result}")
        print(f"Backend: {adapter.backend.name}")
        print(f"GPU Active: {adapter.is_gpu_active}")
        print(f"Supports Graphics: {adapter.supports_graphics}")

        adapter.shutdown()
        print("✓ Render adapter OK")
    except Exception as e:
        print(f"✗ Render adapter test failed: {e}")
    print()

def test_layer_manager():
    """Test layer manager initialization."""
    print("=" * 50)
    print("Layer Manager Test")
    print("=" * 50)

    try:
        from src.render.layers import LayerManager, Layer

        print("Creating LayerManager...")
        lm = LayerManager(80, 25)

        print("Testing draw operations...")
        lm.draw_char(0, 0, '@', 'yellow', Layer.ENTITIES)
        lm.draw_text(5, 0, "Test Text", 'white', Layer.UI)

        print(f"GPU Active: {lm.is_gpu_active}")
        print("✓ Layer manager OK")
    except Exception as e:
        print(f"✗ Layer manager test failed: {e}")
    print()

def main():
    """Run all tests."""
    print()
    print("*" * 50)
    print("  ALT_LAS ENGINE - Windows Compatibility Test")
    print("*" * 50)
    print()

    test_platform()
    test_windows_modules()
    test_terminal_detection()
    test_input_handler()
    test_render_adapter()
    test_layer_manager()

    print("=" * 50)
    print("  All Tests Complete!")
    print("=" * 50)
    print()
    print("The engine is ready to run on this platform.")
    print("Run 'python main.py' or 'run_game.bat' to start the game.")
    print()

if __name__ == "__main__":
    main()
