"""
ALT_LAS Engine - Content Management Handlers
Handles dialogues, characters, and content listing.
"""

from src.api.response import success, error


def handle_create_dialogue(params, content):
    name = params.get("name")
    if not name:
        return error("MISSING_PARAM", "name required")
    nodes = params.get("nodes")
    if not nodes:
        return error("MISSING_PARAM", "nodes required")
    data = {"id": name, "nodes": nodes}
    content.add_dialogue(name, data)
    return success({"name": name}, f"Dialogue '{name}' created")


def handle_create_character(params, content):
    name = params.get("name")
    if not name:
        return error("MISSING_PARAM", "name required")
    data = {
        "name": name,
        "char": params.get("char", "E"),
        "color": params.get("color", "#ff0000"),
        "sprite": params.get("sprite", ""),
        "hp": params.get("hp", 20),
        "attack": params.get("attack", 3),
        "defense": params.get("defense", 1),
        "exp_reward": params.get("exp_reward", 10),
        "gold_reward": params.get("gold_reward", 5),
        "battle_patterns": params.get("battle_patterns", ["rain"]),
    }
    content.add_character(name, data)
    return success(data, f"Character '{name}' created")


def handle_list_content(params, content):
    return success({
        "maps": content.list_maps(),
        "characters": content.list_characters(),
        "dialogues": content.list_dialogues(),
    }, "Content listed")


def handle_get_dialogue(params, content):
    name = params.get("name")
    if not name:
        return error("MISSING_PARAM", "name required")
    data = content.get_dialogue(name)
    if not data:
        return error("NOT_FOUND", f"Dialogue '{name}' not found")
    return success(data, f"Dialogue '{name}' loaded")


def handle_get_character(params, content):
    name = params.get("name")
    if not name:
        return error("MISSING_PARAM", "name required")
    data = content.get_character(name)
    if not data:
        return error("NOT_FOUND", f"Character '{name}' not found")
    return success(data, f"Character '{name}' loaded")


CONTENT_HANDLERS = {
    "create_dialogue": handle_create_dialogue,
    "create_character": handle_create_character,
    "list_content": handle_list_content,
    "get_dialogue": handle_get_dialogue,
    "get_character": handle_get_character,
}
