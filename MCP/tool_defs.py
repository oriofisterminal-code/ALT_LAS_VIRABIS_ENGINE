"""
ALT_LAS Engine - MCP Tool Definitions
Schema definitions for all MCP tools.
"""

TOOLS = [
    {
        "name": "create_map",
        "description": "Create a new map JSON file with given dimensions and tiles.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "name": {"type": "string", "description": "Map name (filename without .json)"},
                "width": {"type": "integer", "description": "Map width in tiles"},
                "height": {"type": "integer", "description": "Map height in tiles"},
                "fill_borders": {"type": "boolean", "description": "Auto-fill border walls", "default": True},
                "player_spawn": {
                    "type": "object",
                    "properties": {"x": {"type": "integer"}, "y": {"type": "integer"}},
                    "description": "Player spawn coordinates"
                }
            },
            "required": ["name", "width", "height"]
        }
    },
    {
        "name": "add_npc",
        "description": "Add an NPC to an existing map.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "map_name": {"type": "string", "description": "Target map name"},
                "npc_name": {"type": "string", "description": "NPC identifier"},
                "x": {"type": "integer"}, "y": {"type": "integer"},
                "char": {"type": "string", "description": "Display character", "default": "N"},
                "color": {"type": "string", "description": "Hex color", "default": "#00ff00"},
                "dialogue_id": {"type": "string", "description": "Dialogue file reference", "default": ""},
                "interactable": {"type": "boolean", "default": True}
            },
            "required": ["map_name", "npc_name", "x", "y"]
        }
    },
    {
        "name": "add_trigger",
        "description": "Add a trigger zone to an existing map.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "map_name": {"type": "string"},
                "x": {"type": "integer"}, "y": {"type": "integer"},
                "event": {"type": "string", "description": "Event ID (e.g. map:room2, battle:boss, flag:key_found)"}
            },
            "required": ["map_name", "x", "y", "event"]
        }
    },
    {
        "name": "create_dialogue",
        "description": "Create a new dialogue tree JSON file.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "name": {"type": "string", "description": "Dialogue ID"},
                "nodes": {
                    "type": "object",
                    "description": "Dict of node_id -> {speaker, text, next, choices, condition, action}"
                }
            },
            "required": ["name", "nodes"]
        }
    },
    {
        "name": "create_character",
        "description": "Create a character/enemy definition file.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "name": {"type": "string"}, "char": {"type": "string", "default": "E"},
                "color": {"type": "string", "default": "#ff0000"},
                "hp": {"type": "integer", "default": 20},
                "attack": {"type": "integer", "default": 3},
                "defense": {"type": "integer", "default": 1}
            },
            "required": ["name"]
        }
    },
    {
        "name": "list_content",
        "description": "List all maps, characters, and dialogues.",
        "inputSchema": {"type": "object", "properties": {}}
    },
    {
        "name": "get_map",
        "description": "Get the full JSON data of a map.",
        "inputSchema": {
            "type": "object",
            "properties": {"name": {"type": "string"}},
            "required": ["name"]
        }
    },
    {
        "name": "set_tile",
        "description": "Set a tile type at specific coordinates on a map.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "map_name": {"type": "string"},
                "x": {"type": "integer"}, "y": {"type": "integer"},
                "tile_type": {"type": "integer", "description": "0=empty, 1=wall, 2=water, 3=trigger"}
            },
            "required": ["map_name", "x", "y", "tile_type"]
        }
    },
    {
        "name": "remove_npc",
        "description": "Remove an NPC from a map by name.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "map_name": {"type": "string"},
                "npc_name": {"type": "string"}
            },
            "required": ["map_name", "npc_name"]
        }
    },
    {
        "name": "validate_map",
        "description": "Validate a map for common errors (unreachable spawn, missing refs).",
        "inputSchema": {
            "type": "object",
            "properties": {"name": {"type": "string"}},
            "required": ["name"]
        }
    }
]
