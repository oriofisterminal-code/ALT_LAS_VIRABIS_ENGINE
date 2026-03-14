"""
ALT_LAS Engine - Battle Scene
Wraps the BattleSystem into a scene for the state manager.
"""

from Source.Scenes.base_scene import BaseScene
from Source.Logic.battle_system import BattleSystem, BattleActor
from Source.Rendering.layer_manager import draw_text, LAYER_UI
from Source.Entities.player import Player


class BattleScene(BaseScene):
    """Scene wrapper for the battle system."""

    def __init__(self):
        super().__init__()
        self.battle = BattleSystem()
        self._player_ref = None
        self.battle.set_on_end(self._on_battle_end)

    def setup_battle(self, player: Player, enemy_data: dict) -> None:
        self._player_ref = player
        player_actor = BattleActor(
            name="Player",
            hp=player.hp,
            attack=player.attack,
            defense=player.defense,
        )
        enemy_actor = BattleActor(
            name=enemy_data.get("name", "Enemy"),
            hp=enemy_data.get("hp", 20),
            attack=enemy_data.get("attack", 3),
            defense=enemy_data.get("defense", 1),
        )
        self.battle.start_battle(player_actor, enemy_actor)

    def handle_input(self, key: str) -> None:
        self.battle.handle_input(key)

    def update(self, dt: float) -> None:
        self.battle.update(dt)

    def render(self) -> None:
        draw_text(
            2, 1, "--- BATTLE ---", color="red", layer=LAYER_UI
        )
        self.battle.render()

    def _on_battle_end(self, result: str) -> None:
        if self._player_ref and self.battle.player:
            self._player_ref.hp = self.battle.player.hp
        if result == "victory" and self._player_ref:
            self._player_ref.exp += 10
            self._player_ref.gold += 5
        if self.state_manager:
            self.state_manager.pop_scene()
