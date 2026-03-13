"""
ALT_LAS Engine - MCP Server
Model Context Protocol server for AI-driven game management.
Allows AI to create maps, add NPCs, manage dialogues, and test scenes
directly from the terminal without writing code.
"""

import json
import sys
import os

BASE_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, BASE_DIR)

from Source.Core.content_loader import ContentLoader
from MCP.tool_defs import TOOLS

content = ContentLoader()
content.load_all()


def send_response(request_id, result):
    response = {"jsonrpc": "2.0", "id": request_id, "result": result}
    msg = json.dumps(response)
    header = f"Content-Length: {len(msg)}\r\n\r\n"
    sys.stdout.write(header + msg)
    sys.stdout.flush()


def send_error(request_id, code, message):
    response = {
        "jsonrpc": "2.0", "id": request_id,
        "error": {"code": code, "message": message}
    }
    msg = json.dumps(response)
    header = f"Content-Length: {len(msg)}\r\n\r\n"
    sys.stdout.write(header + msg)
    sys.stdout.flush()


def handle_create_map(params):
    name = params["name"]
    w = params["width"]
    h = params["height"]
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
        "triggers": [], "npcs": []
    }
    content.add_map(name, data)
    return {"status": "ok", "message": f"Map '{name}' created ({w}x{h})"}


def handle_add_npc(params):
    map_name = params["map_name"]
    map_data = content.get_map(map_name)
    if not map_data:
        return {"status": "error", "message": f"Map '{map_name}' not found"}
    npc = {
        "name": params["npc_name"],
        "x": params["x"], "y": params["y"],
        "char": params.get("char", "N"),
        "color": params.get("color", "#00ff00"),
        "dialogue_id": params.get("dialogue_id", ""),
        "interactable": params.get("interactable", True)
    }
    if "npcs" not in map_data:
        map_data["npcs"] = []
    map_data["npcs"].append(npc)
    content.add_map(map_name, map_data)
    return {"status": "ok", "message": f"NPC '{npc['name']}' added to '{map_name}'"}


def handle_add_trigger(params):
    map_name = params["map_name"]
    map_data = content.get_map(map_name)
    if not map_data:
        return {"status": "error", "message": f"Map '{map_name}' not found"}
    trigger = {"x": params["x"], "y": params["y"], "event": params["event"]}
    if "triggers" not in map_data:
        map_data["triggers"] = []
    map_data["triggers"].append(trigger)
    content.add_map(map_name, map_data)
    return {"status": "ok", "message": f"Trigger added at ({params['x']},{params['y']})"}


def handle_create_dialogue(params):
    name = params["name"]
    data = {"id": name, "nodes": params["nodes"]}
    content.add_dialogue(name, data)
    return {"status": "ok", "message": f"Dialogue '{name}' created"}


def handle_create_character(params):
    name = params["name"]
    data = {
        "name": name, "char": params.get("char", "E"),
        "color": params.get("color", "#ff0000"),
        "hp": params.get("hp", 20), "attack": params.get("attack", 3),
        "defense": params.get("defense", 1)
    }
    content.add_character(name, data)
    return {"status": "ok", "message": f"Character '{name}' created"}


def handle_list_content(_params):
    return {
        "maps": content.list_maps(),
        "characters": content.list_characters(),
        "dialogues": content.list_dialogues()
    }


def handle_get_map(params):
    data = content.get_map(params["name"])
    if not data:
        return {"status": "error", "message": "Map not found"}
    return data


def handle_set_tile(params):
    map_data = content.get_map(params["map_name"])
    if not map_data:
        return {"status": "error", "message": "Map not found"}
    x, y = params["x"], params["y"]
    tiles = map_data.get("tiles", [])
    if 0 <= y < len(tiles) and 0 <= x < len(tiles[y]):
        tiles[y][x] = params["tile_type"]
        content.add_map(params["map_name"], map_data)
        return {"status": "ok", "message": f"Tile ({x},{y}) set to {params['tile_type']}"}
    return {"status": "error", "message": "Coordinates out of bounds"}


def handle_remove_npc(params):
    map_data = content.get_map(params["map_name"])
    if not map_data:
        return {"status": "error", "message": "Map not found"}
    npcs = map_data.get("npcs", [])
    original_len = len(npcs)
    map_data["npcs"] = [n for n in npcs if n.get("name") != params["npc_name"]]
    if len(map_data["npcs"]) < original_len:
        content.add_map(params["map_name"], map_data)
        return {"status": "ok", "message": f"NPC '{params['npc_name']}' removed"}
    return {"status": "error", "message": "NPC not found"}


def handle_validate_map(params):
    data = content.get_map(params["name"])
    if not data:
        return {"status": "error", "message": "Map not found"}
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
    if not issues:
        return {"status": "ok", "message": "Map is valid!", "issues": []}
    return {"status": "warning", "message": f"{len(issues)} issue(s) found", "issues": issues}


TOOL_HANDLERS = {
    "create_map": handle_create_map,
    "add_npc": handle_add_npc,
    "add_trigger": handle_add_trigger,
    "create_dialogue": handle_create_dialogue,
    "create_character": handle_create_character,
    "list_content": handle_list_content,
    "get_map": handle_get_map,
    "set_tile": handle_set_tile,
    "remove_npc": handle_remove_npc,
    "validate_map": handle_validate_map,
}


def handle_request(request):
    method = request.get("method", "")
    req_id = request.get("id")
    params = request.get("params", {})

    if method == "initialize":
        send_response(req_id, {
            "protocolVersion": "2024-11-05",
            "capabilities": {"tools": {"listChanged": False}},
            "serverInfo": {"name": "alt-las-engine", "version": "1.0.0"}
        })
    elif method == "tools/list":
        send_response(req_id, {"tools": TOOLS})
    elif method == "tools/call":
        tool_name = params.get("name", "")
        tool_args = params.get("arguments", {})
        handler = TOOL_HANDLERS.get(tool_name)
        if handler:
            result = handler(tool_args)
            send_response(req_id, {
                "content": [{"type": "text", "text": json.dumps(result, indent=2)}]
            })
        else:
            send_error(req_id, -32601, f"Unknown tool: {tool_name}")
    elif method == "notifications/initialized":
        pass
    else:
        if req_id is not None:
            send_error(req_id, -32601, f"Method not found: {method}")


def main():
    """Run MCP server on stdin/stdout."""
    buffer = ""
    while True:
        try:
            line = sys.stdin.readline()
            if not line:
                break
            buffer += line
            if "Content-Length:" in buffer:
                parts = buffer.split("\r\n\r\n", 1)
                if len(parts) == 2:
                    header, body_start = parts
                    length = 0
                    for h in header.split("\r\n"):
                        if h.startswith("Content-Length:"):
                            length = int(h.split(":")[1].strip())
                    if len(body_start) >= length:
                        body = body_start[:length]
                        buffer = body_start[length:]
                        request = json.loads(body)
                        handle_request(request)
                    continue
            try:
                request = json.loads(buffer.strip())
                buffer = ""
                handle_request(request)
            except json.JSONDecodeError:
                continue
        except (EOFError, KeyboardInterrupt):
            break


if __name__ == "__main__":
    main()
