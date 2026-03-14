"""
ALT_LAS Engine - State Manager
Manages scene transitions with a stack-based approach.
Supports push/pop for overlay scenes (e.g. dialogue over map).
"""

from typing import Optional


class StateManager:
    """Stack-based scene manager for clean transitions."""

    def __init__(self, engine: "GameEngine"):
        self.engine = engine
        self._scene_stack: list = []
        self._scenes: dict = {}

    @property
    def current_scene(self) -> Optional["BaseScene"]:
        if self._scene_stack:
            return self._scene_stack[-1]
        return None

    def register_scene(self, name: str, scene: "BaseScene") -> None:
        self._scenes[name] = scene
        scene.engine = self.engine
        scene.state_manager = self

    def change_scene(self, name: str) -> None:
        if self.current_scene:
            self.current_scene.on_exit()
        self._scene_stack.clear()
        scene = self._scenes[name]
        self._scene_stack.append(scene)
        scene.on_enter()

    def push_scene(self, name: str) -> None:
        if self.current_scene:
            self.current_scene.on_pause()
        scene = self._scenes[name]
        self._scene_stack.append(scene)
        scene.on_enter()

    def pop_scene(self) -> None:
        if self._scene_stack:
            old = self._scene_stack.pop()
            old.on_exit()
        if self.current_scene:
            self.current_scene.on_resume()

    def get_scene(self, name: str) -> Optional["BaseScene"]:
        return self._scenes.get(name)

    @property
    def stack_depth(self) -> int:
        return len(self._scene_stack)
