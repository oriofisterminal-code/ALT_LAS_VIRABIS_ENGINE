"""
ALT_LAS Engine - Menu Scene
Main menu with options: New Game, Continue, Settings, Quit.
"""

from Source.Scenes.base_scene import BaseScene
from Source.Rendering.layer_manager import draw_text, LAYER_UI


MENU_OPTIONS = ["New Game", "Continue", "Settings", "Quit"]

TITLE_ART = [
    "    _   _   _____   _       _    ___  ",
    "   / \\ | | |_   _| | |     / \\  / __| ",
    "  / _ \\| |__ | |   | |__  / _ \\ \\__ \\ ",
    " / ___ \\ ___|| |   |____|/ ___ \\|___/ ",
    "/_/   \\_\\    |_|        /_/   \\_\\     ",
    "",
    "           U  N  D  E  R               ",
]


class MenuScene(BaseScene):
    """Main menu scene."""

    def __init__(self):
        super().__init__()
        self.selected = 0
        self._has_save = False

    def on_enter(self) -> None:
        if self.engine:
            from Source.Core.save_manager import SaveManager
            sm = SaveManager()
            self._has_save = sm.slot_exists("autosave")

    def handle_input(self, key: str) -> None:
        if key in ("TK_UP", "TK_W"):
            self.selected = (self.selected - 1) % len(MENU_OPTIONS)
        elif key in ("TK_DOWN", "TK_S"):
            self.selected = (self.selected + 1) % len(MENU_OPTIONS)
        elif key in ("TK_Z", "TK_RETURN"):
            self._select_option()

    def _select_option(self) -> None:
        option = MENU_OPTIONS[self.selected]
        if option == "New Game":
            if self.state_manager:
                self.state_manager.change_scene("map")
        elif option == "Continue":
            if self._has_save and self.state_manager:
                map_scene = self.state_manager.get_scene("map")
                if map_scene:
                    map_scene.load_game()
                self.state_manager.change_scene("map")
        elif option == "Quit":
            if self.engine:
                self.engine.is_running = False

    def update(self, dt: float) -> None:
        pass

    def render(self) -> None:
        width = self.engine.width if self.engine else 80
        height = self.engine.height if self.engine else 25

        start_y = 3
        for i, line in enumerate(TITLE_ART):
            x = (width - len(line)) // 2
            draw_text(x, start_y + i, line, color="#ff4444", layer=LAYER_UI)

        menu_y = start_y + len(TITLE_ART) + 2
        for i, option in enumerate(MENU_OPTIONS):
            x = (width - len(option) - 4) // 2
            if i == 1 and not self._has_save:
                color = "#555555"
                prefix = "  "
            elif i == self.selected:
                color = "#ffff00"
                prefix = "> "
            else:
                color = "white"
                prefix = "  "
            draw_text(x, menu_y + i * 2, f"{prefix}{option}", color=color, layer=LAYER_UI)

        footer_y = height - 1
        draw_text(
            2, footer_y, "[Z] Select  [Arrow Keys] Navigate",
            color="#666666", layer=LAYER_UI
        )
