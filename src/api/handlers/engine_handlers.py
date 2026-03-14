"""
ALT_LAS Engine - Engine Control Handlers
Allows AI to control engine state: status, pause, resume, config.
"""

import json
import os
import time
from src.api.response import success, error

BASE_DIR = os.path.dirname(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))

# Reference to the running engine (set by server startup)
_engine_ref = None


def set_engine(engine):
    """Set the engine reference for handlers to use."""
    global _engine_ref
    _engine_ref = engine


def get_engine():
    """Get the current engine reference."""
    return _engine_ref


def handle_engine_status(params, content):
    engine = get_engine()
    if not engine:
        return success({
            "running": False,
            "engine_available": False,
        }, "Engine not initialized")

    current_scene = None
    if engine.state_manager and engine.state_manager.current_scene:
        scene = engine.state_manager.current_scene
        current_scene = scene.__class__.__name__

    return success({
        "running": engine.is_running,
        "engine_available": True,
        "fps": engine.fps,
        "delta_time": round(engine.delta_time, 4),
        "current_fps": int(1.0 / engine.delta_time) if engine.delta_time > 0 else 0,
        "debug_mode": engine.debug_mode,
        "width": engine.width,
        "height": engine.height,
        "current_scene": current_scene,
        "scene_stack_depth": engine.state_manager.stack_depth if engine.state_manager else 0,
    }, "Engine status retrieved")


def handle_engine_pause(params, content):
    engine = get_engine()
    if not engine:
        return error("NO_ENGINE", "Engine not initialized")
    if not engine.is_running:
        return error("NOT_RUNNING", "Engine is not running")
    engine._paused = True
    return success({"paused": True}, "Engine paused")


def handle_engine_resume(params, content):
    engine = get_engine()
    if not engine:
        return error("NO_ENGINE", "Engine not initialized")
    engine._paused = False
    return success({"paused": False}, "Engine resumed")


def handle_engine_get_config(params, content):
    try:
        config_path = os.path.join(BASE_DIR, "config", "settings.json")
        with open(config_path, "r", encoding="utf-8") as f:
            config = json.load(f)
        section = params.get("section")
        if section:
            if section in config:
                return success({section: config[section]}, f"Config section '{section}' loaded")
            return error("NOT_FOUND", f"Config section '{section}' not found")
        return success(config, "Full config loaded")
    except Exception as e:
        return error("CONFIG_ERROR", f"Failed to load config: {str(e)}")


def handle_engine_set_config(params, content):
    section = params.get("section")
    values = params.get("values")
    if not section or not values:
        return error("MISSING_PARAM", "section and values required")
    try:
        config_path = os.path.join(BASE_DIR, "config", "settings.json")
        with open(config_path, "r", encoding="utf-8") as f:
            config = json.load(f)
        if section not in config:
            config[section] = {}
        if isinstance(values, dict):
            config[section].update(values)
        with open(config_path, "w", encoding="utf-8") as f:
            json.dump(config, f, indent=4)
        return success(
            {"section": section, "updated_keys": list(values.keys())},
            f"Config section '{section}' updated",
        )
    except Exception as e:
        return error("CONFIG_ERROR", f"Failed to update config: {str(e)}")


ENGINE_HANDLERS = {
    "engine_status": handle_engine_status,
    "engine_pause": handle_engine_pause,
    "engine_resume": handle_engine_resume,
    "engine_get_config": handle_engine_get_config,
    "engine_set_config": handle_engine_set_config,
}
