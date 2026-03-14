"""
ALT_LAS Engine - Save/Load Handlers
Allows AI to save, load, list, and delete game saves.
"""

from src.core.save import SaveManager
from src.api.response import success, error
from src.api.handlers.engine_handlers import get_engine

_save_manager = SaveManager()


def handle_save_game(params, content):
    slot = params.get("slot", "autosave")

    engine = get_engine()
    if not engine or not engine.state_manager:
        return error("NO_ENGINE", "Engine not initialized")

    map_scene = engine.state_manager.get_scene("map")
    if not map_scene or not hasattr(map_scene, "player"):
        return error("NO_PLAYER", "Player not available for saving")

    data = {
        "player": map_scene.player.to_save_data(),
        "current_map": map_scene._current_map_name if hasattr(map_scene, "_current_map_name") else "",
    }
    filepath = _save_manager.save(slot, data)
    return success({"slot": slot, "filepath": filepath}, f"Game saved to slot '{slot}'")


def handle_load_game(params, content):
    slot = params.get("slot", "autosave")

    if not _save_manager.slot_exists(slot):
        return error("NOT_FOUND", f"Save slot '{slot}' not found")

    engine = get_engine()
    if not engine or not engine.state_manager:
        return error("NO_ENGINE", "Engine not initialized")

    map_scene = engine.state_manager.get_scene("map")
    if not map_scene or not hasattr(map_scene, "player"):
        return error("NO_PLAYER", "Player not available for loading")

    data = _save_manager.load(slot)
    if not data:
        return error("LOAD_ERROR", f"Failed to load slot '{slot}'")

    map_scene.player.from_save_data(data.get("player", {}))
    map_name = data.get("current_map", "start_room")
    if hasattr(map_scene, "load_map"):
        map_scene.load_map(map_name)
        map_scene.player.from_save_data(data.get("player", {}))

    return success({
        "slot": slot,
        "map": map_name,
        "player_hp": map_scene.player.hp,
        "player_level": map_scene.player.level,
    }, f"Game loaded from slot '{slot}'")


def handle_list_saves(params, content):
    saves = _save_manager.list_saves()
    return success({
        "saves": saves,
        "count": len(saves),
    }, f"{len(saves)} save(s) found")


def handle_delete_save(params, content):
    slot = params.get("slot")
    if not slot:
        return error("MISSING_PARAM", "slot required")
    if _save_manager.delete_save(slot):
        return success({"slot": slot}, f"Save slot '{slot}' deleted")
    return error("NOT_FOUND", f"Save slot '{slot}' not found")


SAVE_HANDLERS = {
    "save_game": handle_save_game,
    "load_game": handle_load_game,
    "list_saves": handle_list_saves,
    "delete_save": handle_delete_save,
}
