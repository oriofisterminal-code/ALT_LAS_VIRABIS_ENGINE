"""
ALT_LAS Engine - Player Control Handlers
Allows AI to control the player: move, teleport, get/set stats.
"""

from src.api.response import success, error
from src.api.handlers.engine_handlers import get_engine


def _get_map_scene():
    """Get the MapScene if it's the current scene."""
    engine = get_engine()
    if not engine or not engine.state_manager:
        return None
    current = engine.state_manager.current_scene
    if current and current.__class__.__name__ == "MapScene":
        return current
    # Also check registered scenes
    scene = engine.state_manager.get_scene("map")
    return scene


def _get_player():
    """Get the player entity from the map scene."""
    map_scene = _get_map_scene()
    if map_scene and hasattr(map_scene, "player"):
        return map_scene.player
    return None


def handle_player_get_position(params, content):
    player = _get_player()
    if not player:
        return error("NO_PLAYER", "Player not available (map scene not active)")
    return success({
        "x": player.x,
        "y": player.y,
        "facing": player.facing,
    }, f"Player at ({player.x}, {player.y})")


def handle_player_move(params, content):
    direction = params.get("direction")
    if not direction:
        return error("MISSING_PARAM", "direction required (up/down/left/right)")
    if direction not in ("up", "down", "left", "right"):
        return error("INVALID_VALUE", "direction must be up/down/left/right")

    player = _get_player()
    if not player:
        return error("NO_PLAYER", "Player not available (map scene not active)")

    dx, dy = {"up": (0, -1), "down": (0, 1), "left": (-1, 0), "right": (1, 0)}[direction]
    new_x = player.x + dx
    new_y = player.y + dy

    map_scene = _get_map_scene()
    if map_scene and hasattr(map_scene, "_collision_map") and map_scene._collision_map:
        collision_map = map_scene._collision_map
        if not collision_map.is_walkable(new_x, new_y):
            return success({
                "moved": False,
                "x": player.x, "y": player.y,
                "reason": "collision",
            }, f"Cannot move {direction}: blocked")

    player.set_position(new_x, new_y)
    player.facing = direction
    if map_scene and hasattr(map_scene, "collision_system"):
        map_scene.collision_system.update_entity_position(
            player.entity_id, new_x, new_y
        )
    return success({
        "moved": True,
        "x": new_x, "y": new_y,
        "direction": direction,
    }, f"Player moved {direction} to ({new_x}, {new_y})")


def handle_player_teleport(params, content):
    x = params.get("x")
    y = params.get("y")
    if x is None or y is None:
        return error("MISSING_PARAM", "x and y required")

    player = _get_player()
    if not player:
        return error("NO_PLAYER", "Player not available (map scene not active)")

    player.set_position(x, y)
    map_scene = _get_map_scene()
    if map_scene and hasattr(map_scene, "collision_system"):
        map_scene.collision_system.update_entity_position(
            player.entity_id, x, y
        )
    return success({"x": x, "y": y}, f"Player teleported to ({x}, {y})")


def handle_player_get_stats(params, content):
    player = _get_player()
    if not player:
        return error("NO_PLAYER", "Player not available")
    return success({
        "hp": player.hp,
        "max_hp": player.max_hp,
        "attack": player.attack,
        "defense": player.defense,
        "level": player.level,
        "exp": player.exp,
        "gold": player.gold,
        "soul_color": player.soul_color,
        "inventory_count": len(player.inventory),
        "flags": player.flags,
    }, "Player stats retrieved")


def handle_player_set_stats(params, content):
    player = _get_player()
    if not player:
        return error("NO_PLAYER", "Player not available")

    updated = []
    if "hp" in params:
        player.hp = min(params["hp"], player.max_hp)
        updated.append("hp")
    if "max_hp" in params:
        player.max_hp = params["max_hp"]
        updated.append("max_hp")
    if "attack" in params:
        player.attack = params["attack"]
        updated.append("attack")
    if "defense" in params:
        player.defense = params["defense"]
        updated.append("defense")
    if "level" in params:
        player.level = params["level"]
        updated.append("level")
    if "exp" in params:
        player.exp = params["exp"]
        updated.append("exp")
    if "gold" in params:
        player.gold = params["gold"]
        updated.append("gold")
    if "soul_color" in params:
        player.set_soul_color(params["soul_color"])
        updated.append("soul_color")

    if not updated:
        return error("MISSING_PARAM", "No stats provided to update")

    return success({
        "updated": updated,
        "hp": player.hp,
        "max_hp": player.max_hp,
        "attack": player.attack,
        "defense": player.defense,
    }, f"Updated: {', '.join(updated)}")


def handle_player_add_item(params, content):
    player = _get_player()
    if not player:
        return error("NO_PLAYER", "Player not available")
    item = params.get("item")
    if not item:
        return error("MISSING_PARAM", "item required (dict with 'name' key)")
    if isinstance(item, str):
        item = {"name": item}
    player.add_item(item)
    return success({"item": item, "inventory_count": len(player.inventory)},
                   f"Item '{item.get('name', '?')}' added")


def handle_player_get_inventory(params, content):
    player = _get_player()
    if not player:
        return error("NO_PLAYER", "Player not available")
    return success({
        "inventory": player.inventory,
        "count": len(player.inventory),
    }, f"{len(player.inventory)} items in inventory")


def handle_player_set_flag(params, content):
    player = _get_player()
    if not player:
        return error("NO_PLAYER", "Player not available")
    flag = params.get("flag")
    if not flag:
        return error("MISSING_PARAM", "flag required")
    value = params.get("value", True)
    player.set_flag(flag, value)
    return success({"flag": flag, "value": value}, f"Flag '{flag}' set to {value}")


PLAYER_HANDLERS = {
    "player_get_position": handle_player_get_position,
    "player_move": handle_player_move,
    "player_teleport": handle_player_teleport,
    "player_get_stats": handle_player_get_stats,
    "player_set_stats": handle_player_set_stats,
    "player_add_item": handle_player_add_item,
    "player_get_inventory": handle_player_get_inventory,
    "player_set_flag": handle_player_set_flag,
}
