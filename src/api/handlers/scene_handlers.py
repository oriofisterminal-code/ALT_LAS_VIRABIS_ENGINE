"""
ALT_LAS Engine - Scene Management Handlers
Allows AI to control scene transitions: change, push, pop, list, get current.
"""

from src.api.response import success, error
from src.api.handlers.engine_handlers import get_engine


def handle_scene_change(params, content):
    engine = get_engine()
    if not engine or not engine.state_manager:
        return error("NO_ENGINE", "Engine or state manager not initialized")
    scene_name = params.get("scene")
    if not scene_name:
        return error("MISSING_PARAM", "scene required")
    scene = engine.state_manager.get_scene(scene_name)
    if not scene:
        available = list(engine.state_manager._scenes.keys())
        return error("NOT_FOUND",
                     f"Scene '{scene_name}' not found. Available: {available}")
    engine.state_manager.change_scene(scene_name)
    return success({"scene": scene_name}, f"Changed to scene '{scene_name}'")


def handle_scene_get_current(params, content):
    engine = get_engine()
    if not engine or not engine.state_manager:
        return error("NO_ENGINE", "Engine or state manager not initialized")
    current = engine.state_manager.current_scene
    if not current:
        return success({"scene": None, "stack_depth": 0}, "No active scene")
    return success({
        "scene": current.__class__.__name__,
        "stack_depth": engine.state_manager.stack_depth,
    }, f"Current scene: {current.__class__.__name__}")


def handle_scene_list(params, content):
    engine = get_engine()
    if not engine or not engine.state_manager:
        return error("NO_ENGINE", "Engine or state manager not initialized")
    scenes = list(engine.state_manager._scenes.keys())
    current = None
    if engine.state_manager.current_scene:
        for name, scene in engine.state_manager._scenes.items():
            if scene is engine.state_manager.current_scene:
                current = name
                break
    return success({
        "scenes": scenes,
        "current": current,
        "count": len(scenes),
    }, f"{len(scenes)} scenes available")


def handle_scene_push(params, content):
    engine = get_engine()
    if not engine or not engine.state_manager:
        return error("NO_ENGINE", "Engine or state manager not initialized")
    scene_name = params.get("scene")
    if not scene_name:
        return error("MISSING_PARAM", "scene required")
    scene = engine.state_manager.get_scene(scene_name)
    if not scene:
        return error("NOT_FOUND", f"Scene '{scene_name}' not found")
    engine.state_manager.push_scene(scene_name)
    return success({
        "scene": scene_name,
        "stack_depth": engine.state_manager.stack_depth,
    }, f"Pushed scene '{scene_name}'")


def handle_scene_pop(params, content):
    engine = get_engine()
    if not engine or not engine.state_manager:
        return error("NO_ENGINE", "Engine or state manager not initialized")
    if engine.state_manager.stack_depth <= 1:
        return error("STACK_EMPTY", "Cannot pop the last scene from stack")
    engine.state_manager.pop_scene()
    current = engine.state_manager.current_scene
    current_name = current.__class__.__name__ if current else None
    return success({
        "current_scene": current_name,
        "stack_depth": engine.state_manager.stack_depth,
    }, "Scene popped")


SCENE_HANDLERS = {
    "scene_change": handle_scene_change,
    "scene_get_current": handle_scene_get_current,
    "scene_list": handle_scene_list,
    "scene_push": handle_scene_push,
    "scene_pop": handle_scene_pop,
}
