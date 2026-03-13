"""
ALT_LAS Engine - Terminal Map Editor
A simple terminal-based map editor for creating and editing maps.
Run standalone: python -m Editor.map_editor
"""

import json
import os
import sys

BASE_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
MAPS_DIR = os.path.join(BASE_DIR, "Content", "Maps")

TILE_DISPLAY = {0: ".", 1: "#", 2: "~", 3: "T"}
TILE_NAMES = {0: "empty", 1: "wall", 2: "water", 3: "trigger"}


def print_map(data):
    tiles = data.get("tiles", [])
    npcs = {(n["x"], n["y"]): n for n in data.get("npcs", [])}
    triggers = {(t["x"], t["y"]): t for t in data.get("triggers", [])}
    spawn = data.get("player_spawn", {})
    spawn_pos = (spawn.get("x", -1), spawn.get("y", -1))

    print(f"\n  Map: {data.get('name', '?')} ({data.get('width')}x{data.get('height')})")
    print("  " + "-" * (data.get("width", 0) + 2))
    for y, row in enumerate(tiles):
        line = "  |"
        for x, tile in enumerate(row):
            if (x, y) == spawn_pos:
                line += "@"
            elif (x, y) in npcs:
                line += npcs[(x, y)].get("char", "N")
            elif (x, y) in triggers:
                line += "T"
            else:
                line += TILE_DISPLAY.get(tile, "?")
        line += "|"
        print(line)
    print("  " + "-" * (data.get("width", 0) + 2))
    print(f"  Spawn: ({spawn_pos[0]}, {spawn_pos[1]})")
    print(f"  NPCs: {len(data.get('npcs', []))}")
    print(f"  Triggers: {len(data.get('triggers', []))}")


def create_map():
    name = input("Map name: ").strip()
    if not name:
        print("Cancelled.")
        return
    try:
        w = int(input("Width: "))
        h = int(input("Height: "))
    except ValueError:
        print("Invalid dimensions.")
        return
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
    filepath = os.path.join(MAPS_DIR, f"{name}.json")
    with open(filepath, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2)
    print(f"Map '{name}' created at {filepath}")
    print_map(data)


def edit_map():
    maps = [f[:-5] for f in os.listdir(MAPS_DIR) if f.endswith(".json")]
    if not maps:
        print("No maps found.")
        return
    print("Available maps:")
    for i, m in enumerate(maps):
        print(f"  {i + 1}. {m}")
    try:
        idx = int(input("Select map #: ")) - 1
        name = maps[idx]
    except (ValueError, IndexError):
        print("Invalid selection.")
        return
    filepath = os.path.join(MAPS_DIR, f"{name}.json")
    with open(filepath, "r", encoding="utf-8") as f:
        data = json.load(f)
    print_map(data)
    while True:
        print("\n[t]ile  [n]pc  [r]trigger  [s]pawn  [p]rint  [v]alidate  [q]uit")
        cmd = input("> ").strip().lower()
        if cmd == "q":
            break
        elif cmd == "p":
            print_map(data)
        elif cmd == "t":
            _edit_tile(data)
        elif cmd == "n":
            _add_npc(data)
        elif cmd == "r":
            _add_trigger(data)
        elif cmd == "s":
            _set_spawn(data)
        elif cmd == "v":
            _validate(data)
    with open(filepath, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2)
    print(f"Map '{name}' saved.")


def _edit_tile(data):
    try:
        x = int(input("X: "))
        y = int(input("Y: "))
        print("Tile types: 0=empty, 1=wall, 2=water, 3=trigger")
        t = int(input("Type: "))
    except ValueError:
        print("Invalid input.")
        return
    tiles = data.get("tiles", [])
    if 0 <= y < len(tiles) and 0 <= x < len(tiles[y]):
        tiles[y][x] = t
        print(f"Tile ({x},{y}) set to {TILE_NAMES.get(t, '?')}")
    else:
        print("Out of bounds.")


def _add_npc(data):
    name = input("NPC name: ").strip()
    try:
        x = int(input("X: "))
        y = int(input("Y: "))
    except ValueError:
        print("Invalid input.")
        return
    char = input("Char [N]: ").strip() or "N"
    color = input("Color [#00ff00]: ").strip() or "#00ff00"
    dialogue = input("Dialogue ID []: ").strip()
    npc = {
        "name": name, "x": x, "y": y, "char": char,
        "color": color, "dialogue_id": dialogue, "interactable": True
    }
    if "npcs" not in data:
        data["npcs"] = []
    data["npcs"].append(npc)
    print(f"NPC '{name}' added at ({x},{y})")


def _add_trigger(data):
    try:
        x = int(input("X: "))
        y = int(input("Y: "))
    except ValueError:
        print("Invalid input.")
        return
    event = input("Event ID (e.g. map:room2, battle:boss): ").strip()
    if "triggers" not in data:
        data["triggers"] = []
    data["triggers"].append({"x": x, "y": y, "event": event})
    print(f"Trigger added at ({x},{y}) -> {event}")


def _set_spawn(data):
    try:
        x = int(input("Spawn X: "))
        y = int(input("Spawn Y: "))
    except ValueError:
        print("Invalid input.")
        return
    data["player_spawn"] = {"x": x, "y": y}
    print(f"Spawn set to ({x},{y})")


def _validate(data):
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
        print("  Map is valid!")


def main():
    print("=== ALT_LAS Map Editor ===")
    while True:
        print("\n[c]reate  [e]dit  [l]ist  [q]uit")
        cmd = input("> ").strip().lower()
        if cmd == "q":
            break
        elif cmd == "c":
            create_map()
        elif cmd == "e":
            edit_map()
        elif cmd == "l":
            maps = [f[:-5] for f in os.listdir(MAPS_DIR) if f.endswith(".json")]
            print("Maps:", ", ".join(maps) if maps else "(none)")


if __name__ == "__main__":
    main()
