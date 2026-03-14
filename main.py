"""
ALT_LAS Engine - Main Entry Point
Launches the game with all systems initialized.
Usage: python main.py
"""

import sys
import os

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from Source.Core.engine import GameEngine
from Source.Core.state_manager import StateManager
from Source.Core.content_loader import ContentLoader
from Source.Scenes.menu_scene import MenuScene
from Source.Scenes.map_scene import MapScene
from Source.Scenes.battle_scene import BattleScene
from Source.Scenes.dialogue_scene import DialogueScene


def main():
    """Initialize and run the game."""
    content = ContentLoader()
    content.load_all()

    engine = GameEngine()
    state_manager = StateManager(engine)
    engine.set_state_manager(state_manager)

    menu = MenuScene()
    map_scene = MapScene(content)
    battle = BattleScene()
    dialogue = DialogueScene(content)

    state_manager.register_scene("menu", menu)
    state_manager.register_scene("map", map_scene)
    state_manager.register_scene("battle", battle)
    state_manager.register_scene("dialogue", dialogue)

    state_manager.change_scene("menu")
    engine.run()


if __name__ == "__main__":
    main()
