"""
ALT_LAS Engine - Layer Manager
Manages BearLibTerminal's rendering layers (Z-index system).
Each layer serves a purpose: map, entities, effects, UI.
"""

from bearlibterminal import terminal


LAYER_MAP = 0
LAYER_ENTITIES = 1
LAYER_EFFECTS = 2
LAYER_UI = 3


class LayerManager:
    """Static utility for drawing on specific terminal layers."""

    @staticmethod
    def set_layer(layer: int) -> None:
        terminal.layer(layer)

    @staticmethod
    def draw_char(
        x: int, y: int, char: str,
        color: str = "white", layer: int = LAYER_ENTITIES
    ) -> None:
        terminal.layer(layer)
        terminal.printf(x, y, f"[color={color}]{char}[/color]")

    @staticmethod
    def draw_text(
        x: int, y: int, text: str,
        color: str = "white", layer: int = LAYER_UI
    ) -> None:
        terminal.layer(layer)
        terminal.printf(x, y, f"[color={color}]{text}[/color]")

    @staticmethod
    def draw_box(
        x: int, y: int, w: int, h: int,
        border_color: str = "#aaaaaa",
        fill_color: str = "#111111",
        layer: int = LAYER_UI
    ) -> None:
        terminal.layer(layer)
        for bx in range(x, x + w):
            for by in range(y, y + h):
                if bx == x or bx == x + w - 1 or by == y or by == y + h - 1:
                    if bx == x and by == y:
                        ch = "+"
                    elif bx == x + w - 1 and by == y:
                        ch = "+"
                    elif bx == x and by == y + h - 1:
                        ch = "+"
                    elif bx == x + w - 1 and by == y + h - 1:
                        ch = "+"
                    elif by == y or by == y + h - 1:
                        ch = "-"
                    else:
                        ch = "|"
                    terminal.printf(bx, by, f"[color={border_color}]{ch}[/color]")
                else:
                    terminal.printf(bx, by, f"[bkcolor={fill_color}] [/bkcolor]")

    @staticmethod
    def draw_bar(
        x: int, y: int, width: int,
        value: int, max_value: int,
        filled_color: str = "red",
        empty_color: str = "#333333",
        layer: int = LAYER_UI
    ) -> None:
        terminal.layer(layer)
        if max_value <= 0:
            return
        filled = int((value / max_value) * width)
        filled = max(0, min(filled, width))
        for i in range(width):
            color = filled_color if i < filled else empty_color
            terminal.printf(x + i, y, f"[color={color}]|[/color]")

    @staticmethod
    def clear_layer(layer: int) -> None:
        terminal.layer(layer)
        terminal.clear_area(0, 0, terminal.state(terminal.TK_WIDTH),
                            terminal.state(terminal.TK_HEIGHT))
