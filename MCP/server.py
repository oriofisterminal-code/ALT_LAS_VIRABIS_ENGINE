"""
ALT_LAS Engine - MCP Server
Model Context Protocol server for AI-driven game management.
Allows AI to create maps, add NPCs, manage dialogues, manage sprites, and test scenes.
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


# ============================================
# Map Management Handlers
# ============================================

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
        "triggers": [], "npcs": [],
        "tile_sprites": {}  # NEW: sprite mapping
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
        "sprite": params.get("sprite", ""),  # NEW: sprite support
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
        "name": name,
        "char": params.get("char", "E"),
        "color": params.get("color", "#ff0000"),
        "sprite": params.get("sprite", ""),  # NEW: sprite support
        "hp": params.get("hp", 20),
        "attack": params.get("attack", 3),
        "defense": params.get("defense", 1),
        "exp_reward": params.get("exp_reward", 10),
        "gold_reward": params.get("gold_reward", 5),
        "battle_patterns": params.get("battle_patterns", ["rain"])
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
        return {"status": "ok", "message": "Map is valid!", "issues": []}
    return {"status": "warning", "message": f"{len(issues)} issue(s) found", "issues": issues}


# ============================================
# Sprite Management Handlers (NEW)
# ============================================

def handle_set_tile_sprite(params):
    """Assign a sprite to a tile type for a specific map."""
    map_name = params["map_name"]
    tile_type = params["tile_type"]
    sprite_path = params["sprite_path"]

    map_data = content.get_map(map_name)
    if not map_data:
        return {"status": "error", "message": f"Map '{map_name}' not found"}

    if "tile_sprites" not in map_data:
        map_data["tile_sprites"] = {}

    map_data["tile_sprites"][str(tile_type)] = sprite_path
    content.add_map(map_name, map_data)

    tile_names = {0: "empty", 1: "wall", 2: "water", 3: "trigger"}
    tile_name = tile_names.get(tile_type, f"type {tile_type}")
    return {"status": "ok", "message": f"Tile '{tile_name}' sprite set to '{sprite_path}' in map '{map_name}'"}


def handle_set_npc_sprite(params):
    """Assign a sprite to an NPC on a map."""
    map_name = params["map_name"]
    npc_name = params["npc_name"]
    sprite_path = params["sprite_path"]

    map_data = content.get_map(map_name)
    if not map_data:
        return {"status": "error", "message": f"Map '{map_name}' not found"}

    npcs = map_data.get("npcs", [])
    found = False
    for npc in npcs:
        if npc.get("name") == npc_name:
            npc["sprite"] = sprite_path
            found = True
            break

    if not found:
        return {"status": "error", "message": f"NPC '{npc_name}' not found in map '{map_name}'"}

    content.add_map(map_name, map_data)
    return {"status": "ok", "message": f"NPC '{npc_name}' sprite set to '{sprite_path}'"}


def handle_list_sprites(params):
    """List all available sprites in Content/Textures/."""
    textures_dir = os.path.join(BASE_DIR, "Content", "Textures")
    category = params.get("category", "")

    result = {"sprites": [], "categories": {}}

    if not os.path.exists(textures_dir):
        return {"status": "ok", "sprites": [], "categories": {}, "message": "Textures directory not found"}

    categories = ["Tiles", "NPCs", "Battle", "UI"]

    for cat in categories:
        cat_path = os.path.join(textures_dir, cat)
        if os.path.exists(cat_path):
            sprites = []
            for filename in os.listdir(cat_path):
                if filename.lower().endswith((".png", ".jpg", ".jpeg", ".gif")):
                    sprite_entry = {
                        "path": f"{cat}/{filename}",
                        "name": filename,
                        "category": cat
                    }
                    sprites.append(sprite_entry)
                    if not category or category == cat:
                        result["sprites"].append(sprite_entry)
            result["categories"][cat] = len(sprites)

    return result


def handle_validate_sprite(params):
    """Validate a sprite file for correct format and dimensions."""
    sprite_path = params["sprite_path"]
    expected_size = params.get("expected_size", "any")

    full_path = os.path.join(BASE_DIR, "Content", "Textures", sprite_path)

    if not os.path.exists(full_path):
        return {"status": "error", "message": f"Sprite '{sprite_path}' not found"}

    try:
        from PIL import Image
        with Image.open(full_path) as img:
            width, height = img.size
            mode = img.mode
            format_name = img.format

            issues = []
            warnings = []

            # Check size
            if expected_size != "any":
                try:
                    exp_w, exp_h = map(int, expected_size.lower().split("x"))
                    if width != exp_w or height != exp_h:
                        issues.append(f"Size mismatch: expected {expected_size}, got {width}x{height}")
                except ValueError:
                    warnings.append(f"Invalid expected_size format: {expected_size}")

            # Check format
            if mode not in ("RGBA", "RGB", "P"):
                warnings.append(f"Unusual color mode: {mode}")

            result = {
                "status": "ok" if not issues else "warning",
                "message": "Sprite validated",
                "details": {
                    "path": sprite_path,
                    "size": f"{width}x{height}",
                    "width": width,
                    "height": height,
                    "mode": mode,
                    "format": format_name
                },
                "issues": issues,
                "warnings": warnings
            }

            return result

    except ImportError:
        return {"status": "warning", "message": "PIL not available, basic validation only",
                "details": {"path": sprite_path, "exists": True}}
    except Exception as e:
        return {"status": "error", "message": f"Failed to validate sprite: {str(e)}"}


def handle_create_sprite_placeholder(params):
    """Create a placeholder sprite PNG file."""
    sprite_path = params["sprite_path"]
    width = params.get("width", 16)
    height = params.get("height", 16)
    color = params.get("color", "#ff00ff")
    style = params.get("style", "solid")

    full_path = os.path.join(BASE_DIR, "Content", "Textures", sprite_path)

    # Ensure directory exists
    os.makedirs(os.path.dirname(full_path), exist_ok=True)

    try:
        from PIL import Image, ImageDraw

        # Parse color
        color = color.lstrip("#")
        r, g, b = tuple(int(color[i:i+2], 16) for i in (0, 2, 4))
        rgba_color = (r, g, b, 255)

        img = Image.new("RGBA", (width, height), (0, 0, 0, 0))
        draw = ImageDraw.Draw(img)

        if style == "solid":
            draw.rectangle([0, 0, width-1, height-1], fill=rgba_color)
        elif style == "brick":
            draw.rectangle([0, 0, width-1, height-1], fill=rgba_color)
            # Add brick pattern
            for y in range(0, height, height // 4):
                offset = (y // (height // 4)) % 2 * (width // 2)
                draw.line([(0, y), (width-1, y)], fill=(0, 0, 0, 100), width=1)
            for x in range(0, width, width // 2):
                for y in range(0, height, height // 2):
                    draw.line([(x, y), (x, y + height // 4)], fill=(0, 0, 0, 100), width=1)
        elif style == "circle":
            draw.ellipse([0, 0, width-1, height-1], fill=rgba_color)
        else:
            draw.rectangle([0, 0, width-1, height-1], fill=rgba_color)

        img.save(full_path)
        return {
            "status": "ok",
            "message": f"Placeholder sprite created at '{sprite_path}'",
            "details": {"path": sprite_path, "size": f"{width}x{height}", "style": style}
        }

    except ImportError:
        return {"status": "error", "message": "PIL not available for sprite creation"}
    except Exception as e:
        return {"status": "error", "message": f"Failed to create sprite: {str(e)}"}


# ============================================
# Shader & Effects Handlers (NEW)
# ============================================

def handle_set_ambient_light(params):
    """Set the global ambient light color."""
    r = params.get("r", 0.3)
    g = params.get("g", 0.3)
    b = params.get("b", 0.4)

    try:
        from Source.Rendering.layer_manager import get_layer_manager
        lm = get_layer_manager()
        lm.set_ambient_light(r, g, b)
        return {
            "status": "ok",
            "message": f"Ambient light set to RGB({r:.2f}, {g:.2f}, {b:.2f})",
            "details": {"r": r, "g": g, "b": b}
        }
    except Exception as e:
        return {"status": "warning", "message": f"Effect queued (no active renderer): {str(e)}"}


def handle_add_point_light(params):
    """Add a dynamic point light to the scene."""
    x = params["x"]
    y = params["y"]
    radius = params.get("radius", 150)
    color_hex = params.get("color", "#ffffff")
    intensity = params.get("intensity", 1.0)
    name = params.get("name")

    # Parse hex color
    color_hex = color_hex.lstrip("#")
    r, g, b = tuple(int(color_hex[i:i+2], 16) / 255.0 for i in (0, 2, 4))
    color = (r, g, b)

    try:
        from Source.Rendering.layer_manager import get_layer_manager
        lm = get_layer_manager()
        light_id = lm.add_point_light(x, y, radius, color, intensity)
        return {
            "status": "ok",
            "message": f"Point light added at ({x}, {y})",
            "details": {
                "id": light_id,
                "x": x, "y": y,
                "radius": radius,
                "color": params.get("color", "#ffffff"),
                "intensity": intensity
            }
        }
    except Exception as e:
        return {"status": "warning", "message": f"Effect queued (no active renderer): {str(e)}"}


def handle_remove_point_light(params):
    """Remove a point light by name."""
    name = params["name"]

    try:
        from Source.Rendering.layer_manager import get_layer_manager
        lm = get_layer_manager()
        if lm.effects:
            lm.effects.remove_effect(name)
            return {"status": "ok", "message": f"Light '{name}' removed"}
        return {"status": "warning", "message": "No effects manager active"}
    except Exception as e:
        return {"status": "error", "message": f"Failed to remove light: {str(e)}"}


def handle_list_lights(params):
    """List all active point lights."""
    try:
        from Source.Rendering.layer_manager import get_layer_manager
        lm = get_layer_manager()
        if lm.effects:
            lights = lm.effects.get_lights()
            return {
                "status": "ok",
                "lights": lights,
                "count": len(lights)
            }
        return {"status": "ok", "lights": [], "count": 0, "message": "No effects manager active"}
    except Exception as e:
        return {"status": "error", "message": f"Failed to list lights: {str(e)}"}


def handle_set_glow_effect(params):
    """Enable or disable glow/bloom effect."""
    enabled = params["enabled"]
    intensity = params.get("intensity", 1.0)

    try:
        from Source.Rendering.layer_manager import get_layer_manager
        lm = get_layer_manager()
        if lm.effects:
            lm.effects.set_glow_enabled(enabled, intensity)
            return {
                "status": "ok",
                "message": f"Glow effect {'enabled' if enabled else 'disabled'}",
                "details": {"enabled": enabled, "intensity": intensity}
            }
        return {"status": "warning", "message": "No effects manager active"}
    except Exception as e:
        return {"status": "warning", "message": f"Effect queued: {str(e)}"}


def handle_set_water_effect(params):
    """Enable or disable water wave effect."""
    enabled = params["enabled"]
    speed = params.get("speed", 1.0)

    try:
        from Source.Rendering.layer_manager import get_layer_manager
        lm = get_layer_manager()
        if lm.effects:
            lm.effects.set_water_enabled(enabled, speed)
            return {
                "status": "ok",
                "message": f"Water effect {'enabled' if enabled else 'disabled'}",
                "details": {"enabled": enabled, "speed": speed}
            }
        return {"status": "warning", "message": "No effects manager active"}
    except Exception as e:
        return {"status": "warning", "message": f"Effect queued: {str(e)}"}


def handle_spawn_particles(params):
    """Spawn particle effects at a position."""
    x = params["x"]
    y = params["y"]
    count = params.get("count", 10)
    color_hex = params.get("color", "#ffffff")
    lifetime = params.get("lifetime", 1.0)
    speed = params.get("speed", 50.0)

    # Parse hex color
    color_hex = color_hex.lstrip("#")
    r, g, b = tuple(int(color_hex[i:i+2], 16) for i in (0, 2, 4))
    color = (r, g, b, 255)

    try:
        from Source.Rendering.layer_manager import get_layer_manager
        lm = get_layer_manager()
        lm.spawn_particles(x, y, count, color=color, lifetime=lifetime)
        return {
            "status": "ok",
            "message": f"Spawned {count} particles at ({x}, {y})",
            "details": {"x": x, "y": y, "count": count}
        }
    except Exception as e:
        return {"status": "warning", "message": f"Particles queued: {str(e)}"}


def handle_get_render_info(params):
    """Get current render backend and capabilities info."""
    try:
        from Source.Rendering.terminal_detect import get_terminal_capability
        from Source.Rendering.layer_manager import get_layer_manager

        cap = get_terminal_capability()
        lm = get_layer_manager()

        return {
            "status": "ok",
            "render_info": {
                "render_mode": cap.render_mode.value,
                "supports_truecolor": cap.supports_truecolor,
                "supports_sixel": cap.supports_sixel,
                "supports_kitty": cap.supports_kitty,
                "color_depth": cap.color_depth,
                "is_interactive": cap.is_interactive,
                "gpu_active": lm.is_gpu_active,
                "terminal_size": cap.get_terminal_size()
            }
        }
    except Exception as e:
        return {"status": "error", "message": f"Failed to get render info: {str(e)}"}


def handle_set_shader_quality(params):
    """Set shader quality level."""
    quality = params.get("quality", "medium")

    valid_qualities = ["low", "medium", "high"]
    if quality not in valid_qualities:
        return {"status": "error", "message": f"Invalid quality: {quality}. Use: {valid_qualities}"}

    # This would update the config
    try:
        import json
        config_path = os.path.join(BASE_DIR, "Config", "config.json")
        with open(config_path, "r") as f:
            config = json.load(f)

        if "performance" not in config:
            config["performance"] = {}
        config["performance"]["shader_quality"] = quality

        with open(config_path, "w") as f:
            json.dump(config, f, indent=4)

        return {
            "status": "ok",
            "message": f"Shader quality set to '{quality}'",
            "details": {"quality": quality}
        }
    except Exception as e:
        return {"status": "error", "message": f"Failed to set quality: {str(e)}"}


# ============================================
# Tool Handler Registry
# ============================================

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
    # Sprite management
    "set_tile_sprite": handle_set_tile_sprite,
    "set_npc_sprite": handle_set_npc_sprite,
    "list_sprites": handle_list_sprites,
    "validate_sprite": handle_validate_sprite,
    "create_sprite_placeholder": handle_create_sprite_placeholder,
    # Shader & Effects management
    "set_ambient_light": handle_set_ambient_light,
    "add_point_light": handle_add_point_light,
    "remove_point_light": handle_remove_point_light,
    "list_lights": handle_list_lights,
    "set_glow_effect": handle_set_glow_effect,
    "set_water_effect": handle_set_water_effect,
    "spawn_particles": handle_spawn_particles,
    "get_render_info": handle_get_render_info,
    "set_shader_quality": handle_set_shader_quality,
}


def handle_request(request):
    method = request.get("method", "")
    req_id = request.get("id")
    params = request.get("params", {})

    if method == "initialize":
        send_response(req_id, {
            "protocolVersion": "2024-11-05",
            "capabilities": {"tools": {"listChanged": False}},
            "serverInfo": {"name": "alt-las-engine", "version": "1.1.0"}
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
