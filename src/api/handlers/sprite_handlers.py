"""
ALT_LAS Engine - Sprite Management Handlers
Handles sprite assignment, listing, validation, and placeholder creation.
"""

import os
from src.api.response import success, error

BASE_DIR = os.path.dirname(os.path.dirname(os.path.dirname(
    os.path.abspath(__file__))))


def handle_set_tile_sprite(params, content):
    map_name = params.get("map_name")
    if not map_name:
        return error("MISSING_PARAM", "map_name required")
    tile_type = params.get("tile_type")
    sprite_path = params.get("sprite_path")
    if tile_type is None or not sprite_path:
        return error("MISSING_PARAM", "tile_type and sprite_path required")
    map_data = content.get_map(map_name)
    if not map_data:
        return error("NOT_FOUND", f"Map '{map_name}' not found")
    if "tile_sprites" not in map_data:
        map_data["tile_sprites"] = {}
    map_data["tile_sprites"][str(tile_type)] = sprite_path
    content.add_map(map_name, map_data)
    tile_names = {0: "empty", 1: "wall", 2: "water", 3: "trigger"}
    tile_name = tile_names.get(tile_type, f"type {tile_type}")
    return success(
        {"tile_type": tile_type, "sprite_path": sprite_path},
        f"Tile '{tile_name}' sprite set to '{sprite_path}' in map '{map_name}'",
    )


def handle_set_npc_sprite(params, content):
    map_name = params.get("map_name")
    npc_name = params.get("npc_name")
    sprite_path = params.get("sprite_path")
    if not map_name or not npc_name or not sprite_path:
        return error("MISSING_PARAM", "map_name, npc_name, sprite_path required")
    map_data = content.get_map(map_name)
    if not map_data:
        return error("NOT_FOUND", f"Map '{map_name}' not found")
    npcs = map_data.get("npcs", [])
    for npc in npcs:
        if npc.get("name") == npc_name:
            npc["sprite"] = sprite_path
            content.add_map(map_name, map_data)
            return success(
                {"npc_name": npc_name, "sprite_path": sprite_path},
                f"NPC '{npc_name}' sprite set to '{sprite_path}'",
            )
    return error("NOT_FOUND", f"NPC '{npc_name}' not found in map '{map_name}'")


def handle_list_sprites(params, content):
    textures_dir = os.path.join(BASE_DIR, "Content", "Textures")
    category = params.get("category", "")
    result_data = {"sprites": [], "categories": {}}
    if not os.path.exists(textures_dir):
        return success(result_data, "Textures directory not found")
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
                        "category": cat,
                    }
                    sprites.append(sprite_entry)
                    if not category or category == cat:
                        result_data["sprites"].append(sprite_entry)
            result_data["categories"][cat] = len(sprites)
    return success(result_data, f"{len(result_data['sprites'])} sprites found")


def handle_validate_sprite(params, content):
    sprite_path = params.get("sprite_path")
    if not sprite_path:
        return error("MISSING_PARAM", "sprite_path required")
    expected_size = params.get("expected_size", "any")
    full_path = os.path.join(BASE_DIR, "Content", "Textures", sprite_path)
    if not os.path.exists(full_path):
        return error("NOT_FOUND", f"Sprite '{sprite_path}' not found")
    try:
        from PIL import Image
        with Image.open(full_path) as img:
            width, height = img.size
            mode = img.mode
            format_name = img.format
            issues = []
            warnings = []
            if expected_size != "any":
                try:
                    exp_w, exp_h = map(int, expected_size.lower().split("x"))
                    if width != exp_w or height != exp_h:
                        issues.append(f"Size mismatch: expected {expected_size}, got {width}x{height}")
                except ValueError:
                    warnings.append(f"Invalid expected_size format: {expected_size}")
            if mode not in ("RGBA", "RGB", "P"):
                warnings.append(f"Unusual color mode: {mode}")
            return success({
                "path": sprite_path,
                "size": f"{width}x{height}",
                "width": width, "height": height,
                "mode": mode, "format": format_name,
                "issues": issues, "warnings": warnings,
            }, "Sprite validated")
    except ImportError:
        return success({"path": sprite_path, "exists": True},
                       "PIL not available, basic validation only")
    except Exception as e:
        return error("VALIDATION_ERROR", f"Failed to validate: {str(e)}")


def handle_create_sprite_placeholder(params, content):
    sprite_path = params.get("sprite_path")
    if not sprite_path:
        return error("MISSING_PARAM", "sprite_path required")
    width = params.get("width", 16)
    height = params.get("height", 16)
    color = params.get("color", "#ff00ff")
    style = params.get("style", "solid")
    full_path = os.path.join(BASE_DIR, "Content", "Textures", sprite_path)
    os.makedirs(os.path.dirname(full_path), exist_ok=True)
    try:
        from PIL import Image, ImageDraw
        color = color.lstrip("#")
        r, g, b = tuple(int(color[i:i + 2], 16) for i in (0, 2, 4))
        rgba_color = (r, g, b, 255)
        img = Image.new("RGBA", (width, height), (0, 0, 0, 0))
        draw = ImageDraw.Draw(img)
        if style == "brick":
            draw.rectangle([0, 0, width - 1, height - 1], fill=rgba_color)
            for y_pos in range(0, height, max(1, height // 4)):
                draw.line([(0, y_pos), (width - 1, y_pos)], fill=(0, 0, 0, 100), width=1)
        elif style == "circle":
            draw.ellipse([0, 0, width - 1, height - 1], fill=rgba_color)
        else:
            draw.rectangle([0, 0, width - 1, height - 1], fill=rgba_color)
        img.save(full_path)
        return success(
            {"path": sprite_path, "size": f"{width}x{height}", "style": style},
            f"Placeholder sprite created at '{sprite_path}'",
        )
    except ImportError:
        return error("PIL_MISSING", "PIL not available for sprite creation")
    except Exception as e:
        return error("CREATE_ERROR", f"Failed to create sprite: {str(e)}")


SPRITE_HANDLERS = {
    "set_tile_sprite": handle_set_tile_sprite,
    "set_npc_sprite": handle_set_npc_sprite,
    "list_sprites": handle_list_sprites,
    "validate_sprite": handle_validate_sprite,
    "create_sprite_placeholder": handle_create_sprite_placeholder,
}
