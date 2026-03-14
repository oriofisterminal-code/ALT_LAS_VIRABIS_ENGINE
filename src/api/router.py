"""
ALT_LAS Engine - Command Router
Central routing of all API commands to their handlers.
Supports single commands, batch execution, and discovery.
"""

from src.api.response import success, error
from src.api.handlers.map_handlers import MAP_HANDLERS
from src.api.handlers.content_handlers import CONTENT_HANDLERS
from src.api.handlers.sprite_handlers import SPRITE_HANDLERS
from src.api.handlers.effects_handlers import EFFECTS_HANDLERS
from src.api.handlers.engine_handlers import ENGINE_HANDLERS
from src.api.handlers.scene_handlers import SCENE_HANDLERS
from src.api.handlers.player_handlers import PLAYER_HANDLERS
from src.api.handlers.battle_handlers import BATTLE_HANDLERS
from src.api.handlers.save_handlers import SAVE_HANDLERS


# Merge all handler registries
ALL_HANDLERS = {}
ALL_HANDLERS.update(MAP_HANDLERS)
ALL_HANDLERS.update(CONTENT_HANDLERS)
ALL_HANDLERS.update(SPRITE_HANDLERS)
ALL_HANDLERS.update(EFFECTS_HANDLERS)
ALL_HANDLERS.update(ENGINE_HANDLERS)
ALL_HANDLERS.update(SCENE_HANDLERS)
ALL_HANDLERS.update(PLAYER_HANDLERS)
ALL_HANDLERS.update(BATTLE_HANDLERS)
ALL_HANDLERS.update(SAVE_HANDLERS)

# Tool metadata for discovery
TOOL_CATEGORIES = {
    "map": {
        "description": "Map management - create, edit, validate maps",
        "tools": list(MAP_HANDLERS.keys()),
    },
    "content": {
        "description": "Content management - dialogues, characters, listing",
        "tools": list(CONTENT_HANDLERS.keys()),
    },
    "sprite": {
        "description": "Sprite management - assign, list, validate, create sprites",
        "tools": list(SPRITE_HANDLERS.keys()),
    },
    "effects": {
        "description": "Shader & effects - lighting, glow, water, particles",
        "tools": list(EFFECTS_HANDLERS.keys()),
    },
    "engine": {
        "description": "Engine control - status, pause, resume, config",
        "tools": list(ENGINE_HANDLERS.keys()),
    },
    "scene": {
        "description": "Scene management - change, push, pop, list scenes",
        "tools": list(SCENE_HANDLERS.keys()),
    },
    "player": {
        "description": "Player control - move, teleport, stats, inventory, flags",
        "tools": list(PLAYER_HANDLERS.keys()),
    },
    "battle": {
        "description": "Battle control - start, action, get state",
        "tools": list(BATTLE_HANDLERS.keys()),
    },
    "save": {
        "description": "Save/Load - save, load, list, delete game saves",
        "tools": list(SAVE_HANDLERS.keys()),
    },
}

# Tool schemas for AI discovery
TOOL_SCHEMAS = {
    # Map tools
    "create_map": {
        "description": "Create a new map with given dimensions and tiles",
        "params": {"name": "str (required)", "width": "int (required)", "height": "int (required)",
                   "fill_borders": "bool (default: true)", "player_spawn": "dict {x, y}"},
        "example": {"name": "dungeon", "width": 30, "height": 20},
    },
    "get_map": {
        "description": "Get the full JSON data of a map",
        "params": {"name": "str (required)"},
        "example": {"name": "start_room"},
    },
    "set_tile": {
        "description": "Set a tile type at specific coordinates",
        "params": {"map_name": "str", "x": "int", "y": "int", "tile_type": "int (0=empty,1=wall,2=water,3=trigger)"},
        "example": {"map_name": "dungeon", "x": 5, "y": 5, "tile_type": 2},
    },
    "add_npc": {
        "description": "Add an NPC to an existing map",
        "params": {"map_name": "str", "npc_name": "str", "x": "int", "y": "int",
                   "char": "str", "color": "hex str", "sprite": "str", "dialogue_id": "str"},
        "example": {"map_name": "dungeon", "npc_name": "guard", "x": 10, "y": 5},
    },
    "remove_npc": {
        "description": "Remove an NPC from a map by name",
        "params": {"map_name": "str", "npc_name": "str"},
        "example": {"map_name": "dungeon", "npc_name": "guard"},
    },
    "add_trigger": {
        "description": "Add a trigger zone to a map",
        "params": {"map_name": "str", "x": "int", "y": "int", "event": "str (map:X, battle:X, flag:X)"},
        "example": {"map_name": "dungeon", "x": 15, "y": 0, "event": "map:boss_room"},
    },
    "validate_map": {
        "description": "Validate a map for common errors",
        "params": {"name": "str"},
        "example": {"name": "dungeon"},
    },
    # Content tools
    "create_dialogue": {
        "description": "Create a branching dialogue tree",
        "params": {"name": "str", "nodes": "dict of node_id -> {speaker, text, choices, next, condition, action}"},
        "example": {"name": "guard_talk", "nodes": {"start": {"speaker": "Guard", "text": "Halt!"}}},
    },
    "create_character": {
        "description": "Create a character/enemy definition",
        "params": {"name": "str", "hp": "int", "attack": "int", "defense": "int", "sprite": "str"},
        "example": {"name": "Skeleton", "hp": 20, "attack": 4},
    },
    "list_content": {
        "description": "List all maps, characters, and dialogues",
        "params": {},
        "example": {},
    },
    "get_dialogue": {
        "description": "Get dialogue data by name",
        "params": {"name": "str"},
        "example": {"name": "guard_talk"},
    },
    "get_character": {
        "description": "Get character data by name",
        "params": {"name": "str"},
        "example": {"name": "Skeleton"},
    },
    # Sprite tools
    "set_tile_sprite": {
        "description": "Assign a sprite to a tile type for a map",
        "params": {"map_name": "str", "tile_type": "int", "sprite_path": "str"},
        "example": {"map_name": "dungeon", "tile_type": 1, "sprite_path": "Tiles/wall_brick.png"},
    },
    "set_npc_sprite": {
        "description": "Assign a sprite to an NPC on a map",
        "params": {"map_name": "str", "npc_name": "str", "sprite_path": "str"},
        "example": {"map_name": "dungeon", "npc_name": "guard", "sprite_path": "NPCs/guard.png"},
    },
    "list_sprites": {
        "description": "List all available sprites",
        "params": {"category": "str (Tiles/NPCs/Battle/UI, optional)"},
        "example": {"category": "NPCs"},
    },
    "validate_sprite": {
        "description": "Validate a sprite file format and dimensions",
        "params": {"sprite_path": "str", "expected_size": "str (WxH or 'any')"},
        "example": {"sprite_path": "Tiles/wall_brick.png", "expected_size": "16x16"},
    },
    "create_sprite_placeholder": {
        "description": "Create a placeholder sprite PNG",
        "params": {"sprite_path": "str", "width": "int", "height": "int", "color": "hex", "style": "str"},
        "example": {"sprite_path": "NPCs/new_enemy.png", "width": 16, "height": 24, "color": "#ff0000"},
    },
    # Effects tools
    "set_ambient_light": {
        "description": "Set global ambient light color",
        "params": {"r": "float 0-1", "g": "float 0-1", "b": "float 0-1"},
        "example": {"r": 0.5, "g": 0.3, "b": 0.2},
    },
    "add_point_light": {
        "description": "Add a dynamic point light to the scene",
        "params": {"x": "number", "y": "number", "radius": "number", "color": "hex", "intensity": "float"},
        "example": {"x": 100, "y": 100, "radius": 200, "color": "#ffff00"},
    },
    "remove_point_light": {"description": "Remove a point light by name", "params": {"name": "str"}, "example": {"name": "torch_1"}},
    "list_lights": {"description": "List all active point lights", "params": {}, "example": {}},
    "set_glow_effect": {"description": "Enable/disable bloom effect", "params": {"enabled": "bool", "intensity": "float"}, "example": {"enabled": True}},
    "set_water_effect": {"description": "Enable/disable water waves", "params": {"enabled": "bool", "speed": "float"}, "example": {"enabled": True}},
    "spawn_particles": {
        "description": "Spawn particle effects at a position",
        "params": {"x": "number", "y": "number", "count": "int", "color": "hex", "lifetime": "float"},
        "example": {"x": 50, "y": 50, "count": 20, "color": "#ff4400"},
    },
    "get_render_info": {"description": "Get render backend and capabilities", "params": {}, "example": {}},
    "set_shader_quality": {"description": "Set shader quality level", "params": {"quality": "str (low/medium/high)"}, "example": {"quality": "high"}},
    # Engine tools
    "engine_status": {"description": "Get engine status (FPS, scene, render mode)", "params": {}, "example": {}},
    "engine_pause": {"description": "Pause the engine", "params": {}, "example": {}},
    "engine_resume": {"description": "Resume the engine", "params": {}, "example": {}},
    "engine_get_config": {"description": "Get engine config", "params": {"section": "str (optional)"}, "example": {"section": "window"}},
    "engine_set_config": {
        "description": "Update engine config section",
        "params": {"section": "str", "values": "dict"},
        "example": {"section": "window", "values": {"fps": 60}},
    },
    # Scene tools
    "scene_change": {"description": "Change to a different scene", "params": {"scene": "str"}, "example": {"scene": "map"}},
    "scene_get_current": {"description": "Get the current active scene", "params": {}, "example": {}},
    "scene_list": {"description": "List all registered scenes", "params": {}, "example": {}},
    "scene_push": {"description": "Push a scene onto the stack (overlay)", "params": {"scene": "str"}, "example": {"scene": "dialogue"}},
    "scene_pop": {"description": "Pop the top scene from the stack", "params": {}, "example": {}},
    # Player tools
    "player_get_position": {"description": "Get player position and facing direction", "params": {}, "example": {}},
    "player_move": {"description": "Move player in a direction", "params": {"direction": "str (up/down/left/right)"}, "example": {"direction": "right"}},
    "player_teleport": {"description": "Teleport player to coordinates", "params": {"x": "int", "y": "int"}, "example": {"x": 10, "y": 5}},
    "player_get_stats": {"description": "Get player stats (HP, ATK, DEF, etc.)", "params": {}, "example": {}},
    "player_set_stats": {
        "description": "Update player stats",
        "params": {"hp": "int", "max_hp": "int", "attack": "int", "defense": "int", "gold": "int", "level": "int"},
        "example": {"hp": 50, "attack": 10},
    },
    "player_add_item": {"description": "Add item to player inventory", "params": {"item": "str or dict"}, "example": {"item": {"name": "Health Potion", "heal": 20}}},
    "player_get_inventory": {"description": "Get player inventory", "params": {}, "example": {}},
    "player_set_flag": {"description": "Set a game flag on the player", "params": {"flag": "str", "value": "bool"}, "example": {"flag": "boss_defeated", "value": True}},
    # Battle tools
    "battle_start": {"description": "Start a battle against an enemy", "params": {"enemy": "str (character name)"}, "example": {"enemy": "skeleton"}},
    "battle_action": {"description": "Execute a battle action", "params": {"action": "str (fight/act/item/mercy)"}, "example": {"action": "fight"}},
    "battle_get_state": {"description": "Get current battle state", "params": {}, "example": {}},
    # Save tools
    "save_game": {"description": "Save the current game state", "params": {"slot": "str (default: autosave)"}, "example": {"slot": "slot_1"}},
    "load_game": {"description": "Load a saved game", "params": {"slot": "str"}, "example": {"slot": "slot_1"}},
    "list_saves": {"description": "List all saved games", "params": {}, "example": {}},
    "delete_save": {"description": "Delete a saved game", "params": {"slot": "str"}, "example": {"slot": "slot_1"}},
}


class CommandRouter:
    """Routes commands to the appropriate handler."""

    def __init__(self, content):
        self.content = content

    def execute(self, tool: str, args: dict) -> dict:
        """Execute a single command."""
        if tool == "discover":
            return self._handle_discover(args)
        if tool == "batch_execute":
            return self._handle_batch(args)

        handler = ALL_HANDLERS.get(tool)
        if not handler:
            available = sorted(ALL_HANDLERS.keys())
            return error("UNKNOWN_TOOL",
                         f"Unknown tool: '{tool}'. Use 'discover' to see all available tools.",
                         "Unknown tool")
        try:
            return handler(args, self.content)
        except Exception as e:
            return error("HANDLER_ERROR", f"Handler error: {str(e)}")

    def _handle_discover(self, args: dict) -> dict:
        """Return all available tools, categories, and schemas."""
        category_filter = args.get("category")
        if category_filter and category_filter in TOOL_CATEGORIES:
            cat = TOOL_CATEGORIES[category_filter]
            tools = {}
            for tool_name in cat["tools"]:
                if tool_name in TOOL_SCHEMAS:
                    tools[tool_name] = TOOL_SCHEMAS[tool_name]
            return success({
                "category": category_filter,
                "description": cat["description"],
                "tools": tools,
                "count": len(tools),
            }, f"Category '{category_filter}' tools")

        return success({
            "categories": TOOL_CATEGORIES,
            "tools": TOOL_SCHEMAS,
            "total_tools": len(ALL_HANDLERS),
            "special_tools": ["discover", "batch_execute"],
            "api_version": "1.0.0",
            "engine": "ALT_LAS Engine",
        }, f"{len(ALL_HANDLERS)} tools available across {len(TOOL_CATEGORIES)} categories")

    def _handle_batch(self, args: dict) -> dict:
        """Execute multiple commands in sequence."""
        commands = args.get("commands", [])
        if not commands:
            return error("MISSING_PARAM", "commands list required")
        if len(commands) > 50:
            return error("LIMIT_EXCEEDED", "Maximum 50 commands per batch")

        results = []
        for i, cmd in enumerate(commands):
            tool = cmd.get("tool")
            cmd_args = cmd.get("args", {})
            if not tool:
                results.append({"index": i, "result": error("MISSING_PARAM", "tool required in command")})
                continue
            result = self.execute(tool, cmd_args)
            results.append({"index": i, "tool": tool, "result": result})

        succeeded = sum(1 for r in results if r.get("result", {}).get("success"))
        failed = len(results) - succeeded

        return success({
            "results": results,
            "total": len(results),
            "succeeded": succeeded,
            "failed": failed,
        }, f"Batch: {succeeded} succeeded, {failed} failed")
