"""
ALT_LAS Engine - CLI Tool
Command-line interface for managing game content without opening the editor.
Usage: python -m Tools.cli <command> [args]
"""

import json
import os
import sys

BASE_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, BASE_DIR)

from Source.Core.content_loader import ContentLoader

content = ContentLoader()
content.load_all()


def cmd_list(args):
    print("Maps:", ", ".join(content.list_maps()) or "(none)")
    print("Characters:", ", ".join(content.list_characters()) or "(none)")
    print("Dialogues:", ", ".join(content.list_dialogues()) or "(none)")


def cmd_show_map(args):
    if not args:
        print("Usage: show-map <name>")
        return
    data = content.get_map(args[0])
    if not data:
        print(f"Map '{args[0]}' not found.")
        return
    print(json.dumps(data, indent=2))


def cmd_create_map(args):
    if len(args) < 3:
        print("Usage: create-map <name> <width> <height>")
        return
    name, w, h = args[0], int(args[1]), int(args[2])
    tiles = []
    for row in range(h):
        tile_row = []
        for col in range(w):
            if row == 0 or row == h - 1 or col == 0 or col == w - 1:
                tile_row.append(1)
            else:
                tile_row.append(0)
        tiles.append(tile_row)
    data = {
        "name": name, "width": w, "height": h,
        "player_spawn": {"x": 1, "y": 1},
        "tiles": tiles, "triggers": [], "npcs": []
    }
    content.add_map(name, data)
    print(f"Map '{name}' created ({w}x{h})")


def cmd_add_npc(args):
    if len(args) < 4:
        print("Usage: add-npc <map> <name> <x> <y> [char] [color] [dialogue_id]")
        return
    map_name, npc_name = args[0], args[1]
    x, y = int(args[2]), int(args[3])
    char = args[4] if len(args) > 4 else "N"
    color = args[5] if len(args) > 5 else "#00ff00"
    dialogue_id = args[6] if len(args) > 6 else ""
    map_data = content.get_map(map_name)
    if not map_data:
        print(f"Map '{map_name}' not found.")
        return
    npc = {"name": npc_name, "x": x, "y": y, "char": char,
           "color": color, "dialogue_id": dialogue_id, "interactable": True}
    if "npcs" not in map_data:
        map_data["npcs"] = []
    map_data["npcs"].append(npc)
    content.add_map(map_name, map_data)
    print(f"NPC '{npc_name}' added to '{map_name}' at ({x},{y})")


def cmd_validate(args):
    if not args:
        print("Usage: validate <map_name>")
        return
    data = content.get_map(args[0])
    if not data:
        print(f"Map '{args[0]}' not found.")
        return
    issues = []
    spawn = data.get("player_spawn", {})
    sx, sy = spawn.get("x", 0), spawn.get("y", 0)
    tiles = data.get("tiles", [])
    if tiles and 0 <= sy < len(tiles) and 0 <= sx < len(tiles[sy]):
        if tiles[sy][sx] == 1:
            issues.append(f"Spawn ({sx},{sy}) is inside a wall!")
    for npc in data.get("npcs", []):
        nx, ny = npc.get("x", 0), npc.get("y", 0)
        if tiles and 0 <= ny < len(tiles) and 0 <= nx < len(tiles[ny]):
            if tiles[ny][nx] == 1:
                issues.append(f"NPC '{npc.get('name')}' is inside a wall!")
    if issues:
        for issue in issues:
            print(f"  WARNING: {issue}")
    else:
        print("Map is valid!")


COMMANDS = {
    "list": cmd_list,
    "show-map": cmd_show_map,
    "create-map": cmd_create_map,
    "add-npc": cmd_add_npc,
    "validate": cmd_validate,
}


def main():
    if len(sys.argv) < 2 or sys.argv[1] not in COMMANDS:
        print("ALT_LAS CLI - Available commands:")
        for name in COMMANDS:
            print(f"  {name}")
        return
    cmd = sys.argv[1]
    args = sys.argv[2:]
    COMMANDS[cmd](args)


if __name__ == "__main__":
    main()
