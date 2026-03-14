"""
ALT_LAS Engine - Map Management Handlers
Handles map CRUD operations: create, get, set_tile, add/remove NPC, triggers, validation.
"""

import os
from src.api.response import success, error

BASE_DIR = os.path.dirname(os.path.dirname(os.path.dirname(
    os.path.abspath(__file__))))


def handle_create_map(params, content):
    name = params.get("name")
    w = params.get("width")
    h = params.get("height")
    if not name or not w or not h:
        return error("MISSING_PARAM", "name, width, height required")

    fill_borders = params.get("fill_borders", True)
    spawn = params.get("player_spawn", {"x": 1, "y": 1})
    tiles = []
    for row in range(h):
        tile_row = []
        for col in range(w):
            if fill_borders and (row == 0 or row == h - 1 or col == 0 or col == w - 1):
                tile_row.append(1)
            else:
                tile_row.append(0)
        tiles.append(tile_row)
    data = {
        "name": name, "width": w, "height": h,
        "player_spawn": spawn, "tiles": tiles,
        "triggers": [], "npcs": [],
        "tile_sprites": {},
    }
    content.add_map(name, data)
    return success({"name": name, "width": w, "height": h}, f"Map '{name}' created ({w}x{h})")


def handle_get_map(params, content):
    name = params.get("name")
    if not name:
        return error("MISSING_PARAM", "name required")
    data = content.get_map(name)
    if not data:
        return error("NOT_FOUND", f"Map '{name}' not found")
    return success(data, f"Map '{name}' loaded")


def handle_set_tile(params, content):
    map_name = params.get("map_name")
    if not map_name:
        return error("MISSING_PARAM", "map_name required")
    map_data = content.get_map(map_name)
    if not map_data:
        return error("NOT_FOUND", f"Map '{map_name}' not found")
    x, y = params.get("x", -1), params.get("y", -1)
    tile_type = params.get("tile_type")
    if tile_type is None:
        return error("MISSING_PARAM", "tile_type required")
    tiles = map_data.get("tiles", [])
    if 0 <= y < len(tiles) and 0 <= x < len(tiles[y]):
        tiles[y][x] = tile_type
        content.add_map(map_name, map_data)
        return success({"x": x, "y": y, "tile_type": tile_type},
                       f"Tile ({x},{y}) set to {tile_type}")
    return error("OUT_OF_BOUNDS", f"Coordinates ({x},{y}) out of bounds")


def handle_add_npc(params, content):
    map_name = params.get("map_name")
    if not map_name:
        return error("MISSING_PARAM", "map_name required")
    map_data = content.get_map(map_name)
    if not map_data:
        return error("NOT_FOUND", f"Map '{map_name}' not found")
    npc_name = params.get("npc_name")
    if not npc_name:
        return error("MISSING_PARAM", "npc_name required")
    npc = {
        "name": npc_name,
        "x": params.get("x", 0), "y": params.get("y", 0),
        "char": params.get("char", "N"),
        "color": params.get("color", "#00ff00"),
        "sprite": params.get("sprite", ""),
        "dialogue_id": params.get("dialogue_id", ""),
        "interactable": params.get("interactable", True),
    }
    if "npcs" not in map_data:
        map_data["npcs"] = []
    map_data["npcs"].append(npc)
    content.add_map(map_name, map_data)
    return success(npc, f"NPC '{npc_name}' added to '{map_name}'")


def handle_remove_npc(params, content):
    map_name = params.get("map_name")
    if not map_name:
        return error("MISSING_PARAM", "map_name required")
    map_data = content.get_map(map_name)
    if not map_data:
        return error("NOT_FOUND", f"Map '{map_name}' not found")
    npc_name = params.get("npc_name")
    if not npc_name:
        return error("MISSING_PARAM", "npc_name required")
    npcs = map_data.get("npcs", [])
    original_len = len(npcs)
    map_data["npcs"] = [n for n in npcs if n.get("name") != npc_name]
    if len(map_data["npcs"]) < original_len:
        content.add_map(map_name, map_data)
        return success({"npc_name": npc_name}, f"NPC '{npc_name}' removed")
    return error("NOT_FOUND", f"NPC '{npc_name}' not found in map '{map_name}'")


def handle_add_trigger(params, content):
    map_name = params.get("map_name")
    if not map_name:
        return error("MISSING_PARAM", "map_name required")
    map_data = content.get_map(map_name)
    if not map_data:
        return error("NOT_FOUND", f"Map '{map_name}' not found")
    event = params.get("event")
    if not event:
        return error("MISSING_PARAM", "event required")
    trigger = {"x": params.get("x", 0), "y": params.get("y", 0), "event": event}
    if "triggers" not in map_data:
        map_data["triggers"] = []
    map_data["triggers"].append(trigger)
    content.add_map(map_name, map_data)
    return success(trigger, f"Trigger added at ({trigger['x']},{trigger['y']})")


def handle_validate_map(params, content):
    name = params.get("name")
    if not name:
        return error("MISSING_PARAM", "name required")
    data = content.get_map(name)
    if not data:
        return error("NOT_FOUND", f"Map '{name}' not found")
    issues = []
    spawn = data.get("player_spawn", {})
    sx, sy = spawn.get("x", 0), spawn.get("y", 0)
    tiles = data.get("tiles", [])
    if tiles and 0 <= sy < len(tiles) and 0 <= sx < len(tiles[sy]):
        if tiles[sy][sx] == 1:
            issues.append(f"Player spawn ({sx},{sy}) is inside a wall!")
    else:
        issues.append(f"Player spawn ({sx},{sy}) is outside map bounds!")
    for npc in data.get("npcs", []):
        nx, ny = npc.get("x", 0), npc.get("y", 0)
        if tiles and 0 <= ny < len(tiles) and 0 <= nx < len(tiles[ny]):
            if tiles[ny][nx] == 1:
                issues.append(f"NPC '{npc.get('name')}' at ({nx},{ny}) is inside a wall!")
    for trigger in data.get("triggers", []):
        tx, ty = trigger.get("x", 0), trigger.get("y", 0)
        event = trigger.get("event", "")
        if event.startswith("map:"):
            target_map = event.split(":")[1]
            if not content.get_map(target_map):
                issues.append(f"Trigger at ({tx},{ty}) references non-existent map '{target_map}'")
        elif event.startswith("battle:"):
            enemy = event.split(":")[1]
            if not content.get_character(enemy):
                issues.append(f"Trigger at ({tx},{ty}) references non-existent enemy '{enemy}'")
    if not issues:
        return success({"valid": True, "issues": []}, "Map is valid!")
    return success({"valid": False, "issues": issues},
                   f"{len(issues)} issue(s) found")


MAP_HANDLERS = {
    "create_map": handle_create_map,
    "get_map": handle_get_map,
    "set_tile": handle_set_tile,
    "add_npc": handle_add_npc,
    "remove_npc": handle_remove_npc,
    "add_trigger": handle_add_trigger,
    "validate_map": handle_validate_map,
}
