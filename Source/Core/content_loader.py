"""
ALT_LAS Engine - Content Loader
Loads all JSON data files: maps, characters, dialogues.
Single entry point for all game content.
"""

import json
import os
from typing import Optional

BASE_DIR = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
CONTENT_DIR = os.path.join(BASE_DIR, "Content")


def _load_json(filepath: str) -> dict:
    with open(filepath, "r", encoding="utf-8") as f:
        return json.load(f)


class ContentLoader:
    """Centralized content loading from Content/ directory."""

    def __init__(self):
        self._maps: dict[str, dict] = {}
        self._characters: dict[str, dict] = {}
        self._dialogues: dict[str, dict] = {}

    def load_all(self) -> None:
        self._load_directory("Maps", self._maps)
        self._load_directory("Characters", self._characters)
        self._load_directory("Dialogues", self._dialogues)

    def _load_directory(self, subdir: str, target: dict) -> None:
        dir_path = os.path.join(CONTENT_DIR, subdir)
        if not os.path.isdir(dir_path):
            return
        for filename in os.listdir(dir_path):
            if filename.endswith(".json"):
                name = filename[:-5]
                filepath = os.path.join(dir_path, filename)
                try:
                    target[name] = _load_json(filepath)
                except (json.JSONDecodeError, IOError) as e:
                    print(f"[ContentLoader] Error loading {filepath}: {e}")

    def get_map(self, name: str) -> Optional[dict]:
        return self._maps.get(name)

    def get_character(self, name: str) -> Optional[dict]:
        return self._characters.get(name)

    def get_dialogue(self, name: str) -> Optional[dict]:
        return self._dialogues.get(name)

    def list_maps(self) -> list[str]:
        return list(self._maps.keys())

    def list_characters(self) -> list[str]:
        return list(self._characters.keys())

    def list_dialogues(self) -> list[str]:
        return list(self._dialogues.keys())

    def reload_content(self) -> None:
        self._maps.clear()
        self._characters.clear()
        self._dialogues.clear()
        self.load_all()

    def add_map(self, name: str, data: dict) -> None:
        self._maps[name] = data
        filepath = os.path.join(CONTENT_DIR, "Maps", f"{name}.json")
        with open(filepath, "w", encoding="utf-8") as f:
            json.dump(data, f, indent=2)

    def add_character(self, name: str, data: dict) -> None:
        self._characters[name] = data
        filepath = os.path.join(CONTENT_DIR, "Characters", f"{name}.json")
        with open(filepath, "w", encoding="utf-8") as f:
            json.dump(data, f, indent=2)

    def add_dialogue(self, name: str, data: dict) -> None:
        self._dialogues[name] = data
        filepath = os.path.join(CONTENT_DIR, "Dialogues", f"{name}.json")
        with open(filepath, "w", encoding="utf-8") as f:
            json.dump(data, f, indent=2)
