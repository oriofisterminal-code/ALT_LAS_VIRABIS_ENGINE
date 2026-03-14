"""
ALT_LAS Engine - Battle Control Handlers
Allows AI to start battles, select actions, and query battle state.
"""

from src.api.response import success, error
from src.api.handlers.engine_handlers import get_engine


def _get_battle_scene():
    """Get the BattleScene."""
    engine = get_engine()
    if not engine or not engine.state_manager:
        return None
    return engine.state_manager.get_scene("battle")


def handle_battle_start(params, content):
    engine = get_engine()
    if not engine or not engine.state_manager:
        return error("NO_ENGINE", "Engine not initialized")

    enemy_name = params.get("enemy")
    if not enemy_name:
        return error("MISSING_PARAM", "enemy required (character name)")

    char_data = content.get_character(enemy_name)
    if not char_data:
        return error("NOT_FOUND", f"Character '{enemy_name}' not found")

    battle_scene = _get_battle_scene()
    if not battle_scene:
        return error("NO_SCENE", "Battle scene not registered")

    # Get player from map scene
    map_scene = engine.state_manager.get_scene("map")
    if not map_scene or not hasattr(map_scene, "player"):
        return error("NO_PLAYER", "Player not available")

    battle_scene.setup_battle(map_scene.player, char_data)
    engine.state_manager.push_scene("battle")

    return success({
        "enemy": enemy_name,
        "enemy_hp": char_data.get("hp", 20),
        "enemy_attack": char_data.get("attack", 3),
        "player_hp": map_scene.player.hp,
    }, f"Battle started against '{enemy_name}'")


def handle_battle_action(params, content):
    battle_scene = _get_battle_scene()
    if not battle_scene:
        return error("NO_SCENE", "Battle scene not registered")
    if not battle_scene.battle.is_active:
        return error("NO_BATTLE", "No active battle")

    action = params.get("action")
    if not action:
        return error("MISSING_PARAM", "action required (fight/act/item/mercy)")

    action_upper = action.upper()
    valid_actions = ["FIGHT", "ACT", "ITEM", "MERCY"]
    if action_upper not in valid_actions:
        return error("INVALID_VALUE",
                     f"Invalid action: {action}. Use: {[a.lower() for a in valid_actions]}")

    # Set selection and execute
    battle = battle_scene.battle
    try:
        idx = valid_actions.index(action_upper)
        battle.selected_option = idx
        battle._execute_menu_action()
    except Exception as e:
        return error("BATTLE_ERROR", f"Failed to execute action: {str(e)}")

    return success({
        "action": action_upper,
        "phase": battle.phase,
        "player_hp": battle.player.hp if battle.player else 0,
        "enemy_hp": battle.enemy.hp if battle.enemy else 0,
        "enemy_alive": battle.enemy.is_alive if battle.enemy else False,
    }, f"Action '{action_upper}' executed")


def handle_battle_get_state(params, content):
    battle_scene = _get_battle_scene()
    if not battle_scene:
        return error("NO_SCENE", "Battle scene not registered")

    battle = battle_scene.battle
    if not battle.is_active:
        return success({
            "active": False,
            "phase": None,
        }, "No active battle")

    return success({
        "active": True,
        "phase": battle.phase,
        "player": {
            "hp": battle.player.hp if battle.player else 0,
            "max_hp": battle.player.max_hp if battle.player else 0,
            "attack": battle.player.attack if battle.player else 0,
            "alive": battle.player.is_alive if battle.player else False,
        },
        "enemy": {
            "name": battle.enemy.name if battle.enemy else None,
            "hp": battle.enemy.hp if battle.enemy else 0,
            "max_hp": battle.enemy.max_hp if battle.enemy else 0,
            "attack": battle.enemy.attack if battle.enemy else 0,
            "alive": battle.enemy.is_alive if battle.enemy else False,
        },
        "menu_options": battle.menu_options,
        "selected_option": battle.selected_option,
        "soul_position": {"x": battle.soul_x, "y": battle.soul_y},
        "projectile_count": len(battle.projectiles),
        "dodge_timer": round(battle.dodge_timer, 2),
    }, "Battle state retrieved")


BATTLE_HANDLERS = {
    "battle_start": handle_battle_start,
    "battle_action": handle_battle_action,
    "battle_get_state": handle_battle_get_state,
}
