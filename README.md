# ALT_LAS Engine

Terminal-based game engine built with Python + BearLibTerminal, designed with Unreal Engine-like modular architecture.

## Quick Start

```bash
pip install -r requirements.txt
python main.py
```

## Architecture

```
ALT_LAS_ENGINE/
├── Config/               # Window settings, keybindings
├── Content/              # Data-driven game content (no code changes needed)
│   ├── Maps/             # JSON map files
│   ├── Characters/       # NPC/enemy stat definitions
│   ├── Textures/         # Custom ASCII art and fonts
│   └── Dialogues/        # Branching dialogue trees
├── Source/               # Core Python code (max 300 lines per file)
│   ├── Core/             # Engine, StateManager, ContentLoader, SaveManager
│   ├── Physics/          # Collision, Movement
│   ├── Rendering/        # Viewport, Camera, LayerManager
│   ├── Logic/            # DialogueEngine, BattleSystem
│   ├── Scenes/           # BaseScene, MenuScene, MapScene, BattleScene
│   └── Entities/         # Entity, Player, NPC
├── Editor/               # Terminal-based map editor
├── Tools/                # CLI tools for content management
├── MCP/                  # Model Context Protocol server for AI integration
└── main.py               # Entry point
```

## Core Systems

### Engine (Source/Core/engine.py)
- 30 FPS game loop with delta time
- Input handling with configurable keybindings
- Debug mode (F1) showing FPS overlay

### State Manager (Source/Core/state_manager.py)
- Stack-based scene management
- Push/pop for overlay scenes (dialogue over map)
- Clean scene transitions

### Viewport & Camera (Source/Rendering/viewport.py)
- Smooth camera following
- World-to-screen coordinate mapping
- Map boundary clamping

### Layer Manager (Source/Rendering/layer_manager.py)
- 4 render layers: MAP, ENTITIES, EFFECTS, UI
- Draw primitives: char, text, box, progress bar

### Collision System (Source/Physics/collision.py)
- Grid-based collision detection
- Tile types: empty, wall, water, trigger
- Trigger zones that fire events on enter

### Dialogue Engine (Source/Logic/dialogue_engine.py)
- Typewriter text effect
- Branching choices
- Conditional nodes (based on game flags)
- Action callbacks (set flags, give items)

### Battle System (Source/Logic/battle_system.py)
- Turn-based menu: FIGHT, ACT, ITEM, MERCY
- Undertale-style bullet hell dodge phase
- Bullet patterns: rain, spiral, wave

### Content Loader (Source/Core/content_loader.py)
- Loads all JSON content from Content/ directory
- Hot-reload support

### Save System (Source/Core/save_manager.py)
- JSON-based save/load
- Multiple save slots

## Controls

| Key | Action |
|-----|--------|
| Arrow Keys / WASD | Move |
| Z / Enter | Interact / Confirm |
| X | Cancel |
| Escape | Menu |
| I | Inventory |
| F5 | Save |
| F9 | Load |
| F1 | Debug overlay |

## Adding Content (No Code Required)

### New Map
Add a JSON file to `Content/Maps/`:
```json
{
  "name": "my_room",
  "width": 20,
  "height": 15,
  "player_spawn": {"x": 5, "y": 5},
  "tiles": [[1,1,1,...], ...],
  "triggers": [{"x": 10, "y": 5, "event": "map:other_room"}],
  "npcs": [{"name": "npc1", "char": "N", "color": "#00ff00", "x": 8, "y": 8, "dialogue_id": "my_dialogue"}]
}
```

### New Dialogue
Add a JSON file to `Content/Dialogues/`:
```json
{
  "id": "my_dialogue",
  "nodes": {
    "start": {
      "speaker": "NPC Name",
      "text": "Hello there!",
      "choices": [
        {"text": "Hi!", "next": "greeting"},
        {"text": "Bye", "next": null}
      ]
    }
  }
}
```

### New Enemy
Add a JSON file to `Content/Characters/`:
```json
{
  "name": "Goblin",
  "hp": 15,
  "attack": 4,
  "defense": 1
}
```

## MCP Integration (AI Support)

The engine includes an MCP server that allows AI assistants to manage game content directly.

### Setup
Add to your MCP client config:
```json
{
  "mcpServers": {
    "alt-las-engine": {
      "command": "python",
      "args": ["MCP/server.py"],
      "cwd": "/path/to/ALT_LAS_ENGINE"
    }
  }
}
```

### Available MCP Tools
| Tool | Description |
|------|-------------|
| `create_map` | Create a new map with dimensions and auto-borders |
| `add_npc` | Add an NPC to an existing map |
| `add_trigger` | Add a trigger zone to a map |
| `create_dialogue` | Create a dialogue tree |
| `create_character` | Create an enemy/character definition |
| `list_content` | List all maps, characters, dialogues |
| `get_map` | Get full map data |
| `set_tile` | Modify individual tiles |
| `remove_npc` | Remove an NPC from a map |
| `validate_map` | Check map for common errors |

## CLI Tools

```bash
# List all content
python -m Tools.cli list

# Create a map
python -m Tools.cli create-map dungeon 30 20

# Add an NPC
python -m Tools.cli add-npc dungeon guard 5 5 G "#00ff00" guard_dialogue

# Validate a map
python -m Tools.cli validate dungeon
```

## Map Editor

```bash
python -m Editor.map_editor
```

Interactive terminal editor for creating and modifying maps with visual preview.

## Design Principles

1. **No file exceeds 300 lines** - keeps code readable and modular
2. **Data and Logic are separate** - game content lives in JSON, not code
3. **Stack-based scenes** - clean transitions, overlay support
4. **Grid-based physics** - simple, predictable, debuggable
5. **AI-friendly** - MCP server enables AI to build game worlds
