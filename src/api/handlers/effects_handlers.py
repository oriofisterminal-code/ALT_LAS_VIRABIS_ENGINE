"""
ALT_LAS Engine - Shader & Effects Handlers
Handles lighting, glow, water, particles, render info, and shader quality.
"""

import json
import os
from src.api.response import success, error

BASE_DIR = os.path.dirname(os.path.dirname(os.path.dirname(
    os.path.abspath(__file__))))


def handle_set_ambient_light(params, content):
    r = params.get("r", 0.3)
    g = params.get("g", 0.3)
    b = params.get("b", 0.4)
    try:
        from src.Rendering.layer_manager import get_layer_manager
        lm = get_layer_manager()
        lm.set_ambient_light(r, g, b)
        return success({"r": r, "g": g, "b": b},
                       f"Ambient light set to RGB({r:.2f}, {g:.2f}, {b:.2f})")
    except Exception as e:
        return success({"r": r, "g": g, "b": b, "queued": True},
                       f"Effect queued (no active renderer): {str(e)}")


def handle_add_point_light(params, content):
    x = params.get("x")
    y = params.get("y")
    if x is None or y is None:
        return error("MISSING_PARAM", "x and y required")
    radius = params.get("radius", 150)
    color_hex = params.get("color", "#ffffff").lstrip("#")
    intensity = params.get("intensity", 1.0)
    name = params.get("name")
    r, g, b = tuple(int(color_hex[i:i + 2], 16) / 255.0 for i in (0, 2, 4))
    try:
        from src.Rendering.layer_manager import get_layer_manager
        lm = get_layer_manager()
        light_id = lm.add_point_light(x, y, radius, (r, g, b), intensity)
        return success({
            "id": light_id, "x": x, "y": y,
            "radius": radius, "intensity": intensity,
        }, f"Point light added at ({x}, {y})")
    except Exception as e:
        return success({"x": x, "y": y, "queued": True},
                       f"Effect queued: {str(e)}")


def handle_remove_point_light(params, content):
    name = params.get("name")
    if not name:
        return error("MISSING_PARAM", "name required")
    try:
        from src.Rendering.layer_manager import get_layer_manager
        lm = get_layer_manager()
        if lm.effects:
            lm.effects.remove_effect(name)
            return success({"name": name}, f"Light '{name}' removed")
        return success({"name": name}, "No effects manager active")
    except Exception as e:
        return error("EFFECT_ERROR", f"Failed to remove light: {str(e)}")


def handle_list_lights(params, content):
    try:
        from src.Rendering.layer_manager import get_layer_manager
        lm = get_layer_manager()
        if lm.effects:
            lights = lm.effects.get_lights()
            return success({"lights": lights, "count": len(lights)}, "Lights listed")
        return success({"lights": [], "count": 0}, "No effects manager active")
    except Exception as e:
        return error("EFFECT_ERROR", f"Failed to list lights: {str(e)}")


def handle_set_glow_effect(params, content):
    enabled = params.get("enabled")
    if enabled is None:
        return error("MISSING_PARAM", "enabled required")
    intensity = params.get("intensity", 1.0)
    try:
        from src.Rendering.layer_manager import get_layer_manager
        lm = get_layer_manager()
        if lm.effects:
            lm.effects.set_glow_enabled(enabled, intensity)
            return success({"enabled": enabled, "intensity": intensity},
                           f"Glow {'enabled' if enabled else 'disabled'}")
        return success({"enabled": enabled, "queued": True}, "No effects manager active")
    except Exception as e:
        return success({"enabled": enabled, "queued": True}, f"Effect queued: {str(e)}")


def handle_set_water_effect(params, content):
    enabled = params.get("enabled")
    if enabled is None:
        return error("MISSING_PARAM", "enabled required")
    speed = params.get("speed", 1.0)
    try:
        from src.Rendering.layer_manager import get_layer_manager
        lm = get_layer_manager()
        if lm.effects:
            lm.effects.set_water_enabled(enabled, speed)
            return success({"enabled": enabled, "speed": speed},
                           f"Water effect {'enabled' if enabled else 'disabled'}")
        return success({"enabled": enabled, "queued": True}, "No effects manager active")
    except Exception as e:
        return success({"enabled": enabled, "queued": True}, f"Effect queued: {str(e)}")


def handle_spawn_particles(params, content):
    x = params.get("x")
    y = params.get("y")
    if x is None or y is None:
        return error("MISSING_PARAM", "x and y required")
    count = params.get("count", 10)
    color_hex = params.get("color", "#ffffff").lstrip("#")
    lifetime = params.get("lifetime", 1.0)
    speed = params.get("speed", 50.0)
    r, g, b = tuple(int(color_hex[i:i + 2], 16) for i in (0, 2, 4))
    try:
        from src.Rendering.layer_manager import get_layer_manager
        lm = get_layer_manager()
        lm.spawn_particles(x, y, count, color=(r, g, b, 255), lifetime=lifetime)
        return success({"x": x, "y": y, "count": count}, f"Spawned {count} particles at ({x}, {y})")
    except Exception as e:
        return success({"x": x, "y": y, "queued": True}, f"Particles queued: {str(e)}")


def handle_get_render_info(params, content):
    try:
        from src.Rendering.terminal_detect import get_terminal_capability
        from src.Rendering.layer_manager import get_layer_manager
        cap = get_terminal_capability()
        lm = get_layer_manager()
        return success({
            "render_mode": cap.render_mode.value,
            "supports_truecolor": cap.supports_truecolor,
            "supports_sixel": cap.supports_sixel,
            "supports_kitty": cap.supports_kitty,
            "color_depth": cap.color_depth,
            "is_interactive": cap.is_interactive,
            "gpu_active": lm.is_gpu_active,
            "terminal_size": cap.get_terminal_size(),
        }, "Render info retrieved")
    except Exception as e:
        return error("RENDER_ERROR", f"Failed to get render info: {str(e)}")


def handle_set_shader_quality(params, content):
    quality = params.get("quality", "medium")
    valid_qualities = ["low", "medium", "high"]
    if quality not in valid_qualities:
        return error("INVALID_VALUE", f"Invalid quality: {quality}. Use: {valid_qualities}")
    try:
        config_path = os.path.join(BASE_DIR, "Config", "config.json")
        with open(config_path, "r") as f:
            config = json.load(f)
        if "performance" not in config:
            config["performance"] = {}
        config["performance"]["shader_quality"] = quality
        with open(config_path, "w") as f:
            json.dump(config, f, indent=4)
        return success({"quality": quality}, f"Shader quality set to '{quality}'")
    except Exception as e:
        return error("CONFIG_ERROR", f"Failed to set quality: {str(e)}")


EFFECTS_HANDLERS = {
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
