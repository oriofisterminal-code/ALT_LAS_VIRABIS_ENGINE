"""
ALT_LAS Engine - Map Scene
Renders the world map, handles player movement, NPC interaction, triggers.
"""

from Source.Scenes.base_scene import BaseScene
from Source.Rendering.layer_manager import LayerManager, LAYER_MAP, LAYER_ENTITIES, LAYER_UI
from Source.Rendering.viewport import Viewport
from Source.Physics.collision import CollisionMap, CollisionSystem, TILE_WALL, TILE_WATER, TILE_TRIGGER
from Source.Physics.movement import MovementSystem
from Source.Entities.entity import EntityManager
from Source.Entities.player import Player
from Source.Entities.npc import NPC
from Source.Logic.dialogue_engine import DialogueEngine
from Source.Core.content_loader import ContentLoader
from Source.Core.save_manager import SaveManager
import time

TILE_CHARS = {
    0: (".", "#333333"),
    1: ("#", "#808080"),
    2: ("~", "#4444ff"),
    3: (".", "#333333"),
}


class MapScene(BaseScene):
    """Main gameplay scene: map exploration, NPCs, triggers."""

    def __init__(self, content: ContentLoader):
        super().__init__()
        self.content = content
        self.player = Player(x=5, y=5)
        self.entity_manager = EntityManager()
        self.collision_system = CollisionSystem()
        self.movement: MovementSystem = MovementSystem(self.collision_system)
        self.dialogue = DialogueEngine()
        self.viewport = None
        self.save_manager = SaveManager()
        self._collision_map = None
        self._map_data = None
        self._current_map_name = ""
        self._keybindings = None
        self._interaction_target = None
        self._move_time = 0.0

    def on_enter(self) -> None:
        if self.engine:
            self.viewport = Viewport(self.engine)
            self._keybindings = self.engine.keybindings
        self.entity_manager.add(self.player)
        self.collision_system.register_entity(
            self.player.entity_id, self.player.x, self.player.y
        )
        self.collision_system.set_trigger_callback(self._on_trigger)
        self.dialogue.set_action_callback(self._on_dialogue_action)
        self.dialogue.set_game_flags(self.player.flags)
        if not self._current_map_name:
            self.load_map("start_room")

    def load_map(self, map_name: str) -> None:
        data = self.content.get_map(map_name)
        if not data:
            return
        self._map_data = data
        self._current_map_name = map_name
        w = data.get("width", 20)
        h = data.get("height", 15)
        self._collision_map = CollisionMap(w, h)
        tiles = data.get("tiles", [])
        for row_idx, row in enumerate(tiles):
            for col_idx, tile_val in enumerate(row):
                self._collision_map.set_tile(col_idx, row_idx, tile_val)
        for trigger in data.get("triggers", []):
            self._collision_map.set_trigger(
                trigger["x"], trigger["y"], trigger["event"]
            )
        self.collision_system.set_collision_map(self._collision_map)
        if self.viewport:
            self.viewport.set_map_bounds(w, h)
        self.entity_manager.clear()
        self.entity_manager.add(self.player)
        self.collision_system.register_entity(
            self.player.entity_id, self.player.x, self.player.y
        )
        spawn = data.get("player_spawn")
        if spawn:
            self.player.set_position(spawn["x"], spawn["y"])
            self.collision_system.update_entity_position(
                self.player.entity_id, spawn["x"], spawn["y"]
            )
        for npc_data in data.get("npcs", []):
            npc = NPC.from_data(npc_data)
            self.entity_manager.add(npc)
            self.collision_system.register_entity(npc.entity_id, npc.x, npc.y)
        if self.viewport:
            self.viewport.follow(self.player.x, self.player.y)
            self.viewport.camera.snap_to_target()

    def handle_input(self, key: str) -> None:
        if self.dialogue.is_active:
            self.dialogue.handle_input(key)
            return
        kb = self._keybindings or {}
        move_keys = kb.get("movement", {})
        direction = None
        for dir_name, keys in move_keys.items():
            if key in keys:
                direction = dir_name
                break
        if direction:
            result = self.movement.try_move(
                self.player.entity_id, self.player.x, self.player.y,
                direction, time.time()
            )
            if result:
                self.player.set_position(result[0], result[1])
        actions = kb.get("actions", {})
        if key == actions.get("interact"):
            self._interact()
        elif key == actions.get("save"):
            self.save_game()
        elif key == actions.get("menu"):
            if self.state_manager:
                self.state_manager.change_scene("menu")

    def _interact(self) -> None:
        facing_offsets = {"up": (0, -1), "down": (0, 1), "left": (-1, 0), "right": (1, 0)}
        for dx, dy in facing_offsets.values():
            entities = self.entity_manager.get_at(
                self.player.x + dx, self.player.y + dy
            )
            for entity in entities:
                if isinstance(entity, NPC) and entity.interactable:
                    self._start_dialogue(entity)
                    return

    def _start_dialogue(self, npc: NPC) -> None:
        if not npc.dialogue_id:
            return
        dialogue_data = self.content.get_dialogue(npc.dialogue_id)
        if dialogue_data:
            self.dialogue.load_dialogue(dialogue_data)
            self.dialogue.start()

    def _on_trigger(self, event_id: str) -> None:
        if event_id.startswith("map:"):
            parts = event_id.split(":")
            if len(parts) >= 2:
                self.load_map(parts[1])
        elif event_id.startswith("battle:"):
            parts = event_id.split(":")
            if len(parts) >= 2 and self.state_manager:
                battle_scene = self.state_manager.get_scene("battle")
                if battle_scene:
                    enemy_name = parts[1]
                    char_data = self.content.get_character(enemy_name)
                    if char_data:
                        battle_scene.setup_battle(self.player, char_data)
                    self.state_manager.push_scene("battle")
        elif event_id.startswith("flag:"):
            parts = event_id.split(":")
            if len(parts) >= 2:
                self.player.set_flag(parts[1], True)

    def _on_dialogue_action(self, action) -> None:
        if isinstance(action, dict):
            if "set_flag" in action:
                self.player.set_flag(action["set_flag"], True)
            if "give_item" in action:
                self.player.add_item(action["give_item"])

    def update(self, dt: float) -> None:
        self.entity_manager.update_all(dt)
        self.dialogue.update(dt)
        if self.viewport:
            self.viewport.follow(self.player.x, self.player.y)
            self.viewport.update(dt)

    def render(self) -> None:
        self._render_map()
        self._render_entities()
        self._render_hud()
        self.dialogue.render()

    def _render_map(self) -> None:
        if not self._collision_map or not self.viewport:
            return
        for wy in range(self._collision_map.height):
            for wx in range(self._collision_map.width):
                if not self.viewport.is_visible(wx, wy):
                    continue
                sx, sy = self.viewport.world_to_screen(wx, wy)
                tile = self._collision_map.get_tile(wx, wy)
                char, color = TILE_CHARS.get(tile, ("?", "white"))
                LayerManager.draw_char(sx, sy, char, color=color, layer=LAYER_MAP)

    def _render_entities(self) -> None:
        if not self.viewport:
            return
        for entity in self.entity_manager.get_all():
            if not entity.visible:
                continue
            if not self.viewport.is_visible(entity.x, entity.y):
                continue
            sx, sy = self.viewport.world_to_screen(entity.x, entity.y)
            LayerManager.draw_char(
                sx, sy, entity.char, color=entity.color, layer=LAYER_ENTITIES
            )

    def _render_hud(self) -> None:
        h = self.engine.height if self.engine else 25
        LayerManager.draw_text(
            1, h - 1,
            f"HP:{self.player.hp}/{self.player.max_hp} "
            f"LV:{self.player.level} "
            f"G:{self.player.gold} "
            f"Map:{self._current_map_name}",
            color="#aaaaaa", layer=LAYER_UI
        )

    def save_game(self) -> None:
        data = {
            "player": self.player.to_save_data(),
            "current_map": self._current_map_name,
        }
        self.save_manager.save("autosave", data)

    def load_game(self) -> None:
        data = self.save_manager.load("autosave")
        if not data:
            return
        self.player.from_save_data(data.get("player", {}))
        map_name = data.get("current_map", "start_room")
        self.load_map(map_name)
        self.player.from_save_data(data.get("player", {}))
