"""
ALT_LAS Engine - Dialogue Engine
Typewriter text display with branching dialogue trees.
Dialogues are loaded from JSON, not hardcoded.
"""

import time
from typing import Optional
from Source.Rendering.layer_manager import draw_text, draw_box, LAYER_UI


class DialogueNode:
    """Single node in a dialogue tree."""

    def __init__(self, node_data: dict):
        self.speaker = node_data.get("speaker", "")
        self.text = node_data.get("text", "")
        self.choices = node_data.get("choices", [])
        self.next_node = node_data.get("next", None)
        self.condition = node_data.get("condition", None)
        self.action = node_data.get("action", None)


class DialogueEngine:
    """Manages dialogue display with typewriter effect and choices."""

    def __init__(self, box_x: int = 2, box_y: int = 18,
                 box_w: int = 76, box_h: int = 6):
        self.box_x = box_x
        self.box_y = box_y
        self.box_w = box_w
        self.box_h = box_h
        self._nodes: dict[str, DialogueNode] = {}
        self._current_node: Optional[DialogueNode] = None
        self._displayed_text = ""
        self._char_index = 0
        self._last_char_time = 0.0
        self._char_delay = 0.03
        self._is_active = False
        self._text_complete = False
        self._selected_choice = 0
        self._action_callback = None
        self._game_flags: dict[str, bool] = {}

    @property
    def is_active(self) -> bool:
        return self._is_active

    def set_action_callback(self, callback) -> None:
        self._action_callback = callback

    def set_game_flags(self, flags: dict[str, bool]) -> None:
        self._game_flags = flags

    def load_dialogue(self, dialogue_data: dict) -> None:
        self._nodes.clear()
        nodes = dialogue_data.get("nodes", {})
        for nid, ndata in nodes.items():
            self._nodes[nid] = DialogueNode(ndata)

    def start(self, start_node: str = "start") -> None:
        node = self._nodes.get(start_node)
        if not node:
            return
        self._set_current_node(node)
        self._is_active = True

    def _set_current_node(self, node: DialogueNode) -> None:
        if node.condition:
            flag = node.condition.get("flag", "")
            expected = node.condition.get("value", True)
            actual = self._game_flags.get(flag, False)
            if actual != expected:
                alt = node.condition.get("else_node")
                if alt and alt in self._nodes:
                    self._set_current_node(self._nodes[alt])
                    return
                self._is_active = False
                return
        self._current_node = node
        self._displayed_text = ""
        self._char_index = 0
        self._text_complete = False
        self._selected_choice = 0
        self._last_char_time = time.time()

    def update(self, dt: float) -> None:
        if not self._is_active or not self._current_node:
            return
        if self._text_complete:
            return
        now = time.time()
        if now - self._last_char_time >= self._char_delay:
            if self._char_index < len(self._current_node.text):
                self._displayed_text += self._current_node.text[self._char_index]
                self._char_index += 1
                self._last_char_time = now
            else:
                self._text_complete = True

    def handle_input(self, key: str) -> None:
        if not self._is_active or not self._current_node:
            return
        if not self._text_complete:
            self._displayed_text = self._current_node.text
            self._text_complete = True
            return
        node = self._current_node
        if node.choices:
            if key in ("TK_UP", "TK_W"):
                self._selected_choice = max(0, self._selected_choice - 1)
            elif key in ("TK_DOWN", "TK_S"):
                self._selected_choice = min(
                    len(node.choices) - 1, self._selected_choice + 1
                )
            elif key in ("TK_Z", "TK_RETURN"):
                choice = node.choices[self._selected_choice]
                self._fire_action(choice.get("action"))
                next_id = choice.get("next")
                self._advance(next_id)
        elif key in ("TK_Z", "TK_RETURN"):
            self._fire_action(node.action)
            self._advance(node.next_node)

    def _fire_action(self, action) -> None:
        if action and self._action_callback:
            self._action_callback(action)

    def _advance(self, next_id: Optional[str]) -> None:
        if next_id and next_id in self._nodes:
            self._set_current_node(self._nodes[next_id])
        else:
            self._is_active = False
            self._current_node = None

    def render(self) -> None:
        if not self._is_active or not self._current_node:
            return
        draw_box(
            self.box_x, self.box_y, self.box_w, self.box_h,
            border_color="#aaaaaa", fill_color="#111111", layer=LAYER_UI
        )
        node = self._current_node
        if node.speaker:
            draw_text(
                self.box_x + 2, self.box_y,
                f" {node.speaker} ", color="#ffff00", layer=LAYER_UI
            )
        max_text_w = self.box_w - 4
        lines = self._wrap_text(self._displayed_text, max_text_w)
        for i, line in enumerate(lines[:self.box_h - 2]):
            draw_text(
                self.box_x + 2, self.box_y + 1 + i,
                line, color="white", layer=LAYER_UI
            )
        if self._text_complete and node.choices:
            cy = self.box_y + 1 + min(len(lines), 2)
            for i, choice in enumerate(node.choices):
                prefix = "> " if i == self._selected_choice else "  "
                color = "#ffff00" if i == self._selected_choice else "#aaaaaa"
                draw_text(
                    self.box_x + 2, cy + i,
                    f"{prefix}{choice.get('text', '')}",
                    color=color, layer=LAYER_UI
                )

    @staticmethod
    def _wrap_text(text: str, max_width: int) -> list[str]:
        words = text.split(" ")
        lines = []
        current = ""
        for word in words:
            if len(current) + len(word) + 1 <= max_width:
                current = f"{current} {word}" if current else word
            else:
                if current:
                    lines.append(current)
                current = word
        if current:
            lines.append(current)
        return lines
