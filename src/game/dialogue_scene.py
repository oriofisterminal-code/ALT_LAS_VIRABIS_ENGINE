"""
ALT_LAS Engine - Dialogue Scene (Standalone)
For cutscenes or dialogue sequences outside of map context.
"""

from src.game.base_scene import BaseScene
from src.game.dialogue_engine import DialogueEngine
from src.core.content_loader import ContentLoader


class DialogueScene(BaseScene):
    """Standalone dialogue scene for cutscenes."""

    def __init__(self, content: ContentLoader):
        super().__init__()
        self.content = content
        self.dialogue = DialogueEngine()
        self._next_scene = None
        self._action_callback = None

    def setup(self, dialogue_name: str, next_scene: str = None,
              action_callback=None) -> None:
        self._next_scene = next_scene
        self._action_callback = action_callback
        data = self.content.get_dialogue(dialogue_name)
        if data:
            self.dialogue.load_dialogue(data)
            if action_callback:
                self.dialogue.set_action_callback(action_callback)

    def on_enter(self) -> None:
        self.dialogue.start()

    def handle_input(self, key: str) -> None:
        self.dialogue.handle_input(key)
        if not self.dialogue.is_active:
            if self._next_scene and self.state_manager:
                self.state_manager.change_scene(self._next_scene)
            elif self.state_manager:
                self.state_manager.pop_scene()

    def update(self, dt: float) -> None:
        self.dialogue.update(dt)

    def render(self) -> None:
        self.dialogue.render()
