"""
ALT_LAS Engine - MCP Tool Definitions
Schema definitions for all MCP tools including sprite management.
"""

TOOLS = [
    # Map Management
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
                "x": {"type": "integer"},
                "y": {"type": "integer"},
                "char": {"type": "string", "description": "Display character", "default": "N"},
                "color": {"type": "string", "description": "Hex color", "default": "#00ff00"},
                "sprite": {"type": "string", "description": "Sprite path (e.g., NPCs/flowey.png)"},
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
                "x": {"type": "integer"},
                "y": {"type": "integer"},
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
                "name": {"type": "string"},
                "char": {"type": "string", "default": "E"},
                "color": {"type": "string", "default": "#ff0000"},
                "sprite": {"type": "string", "description": "Battle sprite path"},
                "hp": {"type": "integer", "default": 20},
                "attack": {"type": "integer", "default": 3},
                "defense": {"type": "integer", "default": 1}
            },
            "required": ["name"]
        }
    },
    # Content Listing
    {
        "name": "list_content",
        "description": "List all maps, characters, dialogues, and sprites.",
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
                "x": {"type": "integer"},
                "y": {"type": "integer"},
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
    },
    # Sprite Management (NEW)
    {
        "name": "set_tile_sprite",
        "description": "Assign a sprite to a tile type for a specific map.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "map_name": {"type": "string", "description": "Target map name"},
                "tile_type": {"type": "integer", "description": "Tile type (0=empty, 1=wall, 2=water, 3=trigger)"},
                "sprite_path": {"type": "string", "description": "Sprite path relative to Content/Textures/ (e.g., Tiles/wall_brick.png)"}
            },
            "required": ["map_name", "tile_type", "sprite_path"]
        }
    },
    {
        "name": "set_npc_sprite",
        "description": "Assign a sprite to an NPC on a map.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "map_name": {"type": "string", "description": "Target map name"},
                "npc_name": {"type": "string", "description": "NPC name to update"},
                "sprite_path": {"type": "string", "description": "Sprite path relative to Content/Textures/ (e.g., NPCs/flowey.png)"}
            },
            "required": ["map_name", "npc_name", "sprite_path"]
        }
    },
    {
        "name": "list_sprites",
        "description": "List all available sprites in Content/Textures/.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "category": {"type": "string", "description": "Filter by category (Tiles, NPCs, Battle, UI) or leave empty for all"}
            }
        }
    },
    {
        "name": "validate_sprite",
        "description": "Validate a sprite file for correct format and dimensions.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "sprite_path": {"type": "string", "description": "Sprite path relative to Content/Textures/"},
                "expected_size": {"type": "string", "description": "Expected size in WxH format (e.g., '16x16', '32x32') or 'any'"}
            },
            "required": ["sprite_path"]
        }
    },
    {
        "name": "create_sprite_placeholder",
        "description": "Create a placeholder sprite PNG file with specified properties.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "sprite_path": {"type": "string", "description": "Path relative to Content/Textures/ (e.g., NPCs/my_character.png)"},
                "width": {"type": "integer", "description": "Sprite width in pixels", "default": 16},
                "height": {"type": "integer", "description": "Sprite height in pixels", "default": 16},
                "color": {"type": "string", "description": "Fill color as hex (e.g., #ff0000)", "default": "#ff00ff"},
                "style": {"type": "string", "description": "Style type (solid, brick, stone, water, flower, skeleton, guard, simple)", "default": "solid"}
            },
            "required": ["sprite_path"]
        }
    },
    # Shader & Effects Management
    {
        "name": "set_ambient_light",
        "description": "Set the global ambient light color for the current scene.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "r": {"type": "number", "description": "Red component (0.0-1.0)", "default": 0.3},
                "g": {"type": "number", "description": "Green component (0.0-1.0)", "default": 0.3},
                "b": {"type": "number", "description": "Blue component (0.0-1.0)", "default": 0.4}
            },
            "required": []
        }
    },
    {
        "name": "add_point_light",
        "description": "Add a dynamic point light to the scene. Creates localized lighting effects.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "x": {"type": "number", "description": "X position in pixels"},
                "y": {"type": "number", "description": "Y position in pixels"},
                "radius": {"type": "number", "description": "Light radius in pixels", "default": 150},
                "color": {"type": "string", "description": "Light color as hex (e.g., #ffff00)", "default": "#ffffff"},
                "intensity": {"type": "number", "description": "Light intensity (0.0-2.0)", "default": 1.0},
                "name": {"type": "string", "description": "Optional light identifier for later reference"}
            },
            "required": ["x", "y"]
        }
    },
    {
        "name": "remove_point_light",
        "description": "Remove a point light by name.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "name": {"type": "string", "description": "Light identifier to remove"}
            },
            "required": ["name"]
        }
    },
    {
        "name": "list_lights",
        "description": "List all active point lights in the current scene.",
        "inputSchema": {"type": "object", "properties": {}}
    },
    {
        "name": "set_glow_effect",
        "description": "Enable or disable glow/bloom post-processing effect.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "enabled": {"type": "boolean", "description": "Enable or disable glow"},
                "intensity": {"type": "number", "description": "Glow intensity (0.0-2.0)", "default": 1.0}
            },
            "required": ["enabled"]
        }
    },
    {
        "name": "set_water_effect",
        "description": "Enable or disable water wave distortion effect for water tiles.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "enabled": {"type": "boolean", "description": "Enable or disable water waves"},
                "speed": {"type": "number", "description": "Wave animation speed", "default": 1.0}
            },
            "required": ["enabled"]
        }
    },
    {
        "name": "spawn_particles",
        "description": "Spawn particle effects at a position.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "x": {"type": "number", "description": "X position"},
                "y": {"type": "number", "description": "Y position"},
                "count": {"type": "integer", "description": "Number of particles", "default": 10},
                "color": {"type": "string", "description": "Particle color as hex", "default": "#ffffff"},
                "lifetime": {"type": "number", "description": "Particle lifetime in seconds", "default": 1.0},
                "speed": {"type": "number", "description": "Particle speed", "default": 50.0}
            },
            "required": ["x", "y"]
        }
    },
    {
        "name": "get_render_info",
        "description": "Get current render backend and capabilities info.",
        "inputSchema": {"type": "object", "properties": {}}
    },
    {
        "name": "set_shader_quality",
        "description": "Set shader quality level for performance tuning.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "quality": {"type": "string", "description": "Quality level (low, medium, high)", "default": "medium"}
            },
            "required": ["quality"]
        }
    }
]
