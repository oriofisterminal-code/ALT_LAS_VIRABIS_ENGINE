"""
ALT_LAS Engine - Save Manager
Handles save/load of game state to JSON files.
"""

import json
import os
import time
from typing import Optional

BASE_DIR = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SAVE_DIR = os.path.join(BASE_DIR, "Saves")


class SaveManager:
    """Saves and loads game state from JSON files."""

    def __init__(self):
        os.makedirs(SAVE_DIR, exist_ok=True)

    def save(self, slot: str, data: dict) -> str:
        save_data = {
            "slot": slot,
            "timestamp": time.time(),
            "data": data,
        }
        filepath = os.path.join(SAVE_DIR, f"{slot}.json")
        with open(filepath, "w", encoding="utf-8") as f:
            json.dump(save_data, f, indent=2)
        return filepath

    def load(self, slot: str) -> Optional[dict]:
        filepath = os.path.join(SAVE_DIR, f"{slot}.json")
        if not os.path.exists(filepath):
            return None
        with open(filepath, "r", encoding="utf-8") as f:
            save_data = json.load(f)
        return save_data.get("data")

    def list_saves(self) -> list[dict]:
        saves = []
        for filename in os.listdir(SAVE_DIR):
            if filename.endswith(".json"):
                filepath = os.path.join(SAVE_DIR, filename)
                try:
                    with open(filepath, "r", encoding="utf-8") as f:
                        data = json.load(f)
                    saves.append({
                        "slot": data.get("slot", filename[:-5]),
                        "timestamp": data.get("timestamp", 0),
                    })
                except (json.JSONDecodeError, IOError):
                    continue
        saves.sort(key=lambda s: s["timestamp"], reverse=True)
        return saves

    def delete_save(self, slot: str) -> bool:
        filepath = os.path.join(SAVE_DIR, f"{slot}.json")
        if os.path.exists(filepath):
            os.remove(filepath)
            return True
        return False

    def slot_exists(self, slot: str) -> bool:
        filepath = os.path.join(SAVE_DIR, f"{slot}.json")
        return os.path.exists(filepath)
