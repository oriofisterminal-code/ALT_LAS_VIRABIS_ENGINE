# ALT_LAS Engine

> Terminal-based game engine with GPU-accelerated shaders and Undertale-style combat

![Python](https://img.shields.io/badge/Python-3.10+-blue.svg)
![License](https://img.shields.io/badge/License-MIT-green.svg)
![Status](https://img.shields.io/badge/Status-Alpha-orange.svg)

## 🎮 Overview

ALT_LAS Engine is a modular, terminal-based game engine designed for creating Undertale/Stardew Valley-style games that run entirely in the terminal. It features GPU-accelerated rendering via ModernGL, a complete shader system, and full AI integration through MCP (Model Context Protocol).

## ✨ Features

### Rendering System
- **3-Level Fallback Rendering** - GPU Shader → Pre-render → ASCII
- **GLSL Shader Support** - Point lights, ambient lighting, glow/bloom, water effects
- **Terminal Graphics Protocols** - Kitty, Sixel, iTerm2 inline images
- **4-Layer Rendering** - MAP, ENTITIES, EFFECTS, UI layers

### Game Systems
- **Turn-based Combat** - FIGHT / ACT / ITEM / MERCY menu system
- **Bullet Hell Dodge** - Undertale-style soul system with projectile patterns
- **Branching Dialogues** - Conditional nodes, action callbacks, choice system
- **Grid-based Physics** - Simple, predictable collision and movement

### AI Integration
- **MCP Server** - AI can create and modify game content
- **24 Management Tools** - Maps, NPCs, dialogues, sprites, shaders, effects
- **JSON-based Content** - No code changes needed for new content

---

## 📁 Project Architecture

```
ALT_LAS_ENGINE/
│
├── 📂 Config/                    # Configuration files
│   ├── config.json               # Main config (window, GPU, effects)
│   └── keybindings.json          # Key mappings
│
├── 📂 Content/                   # Data-driven game content
│   ├── 📂 Maps/                  # JSON map definitions
│   │   ├── start_room.json
│   │   └── corridor.json
│   ├── 📂 Characters/            # NPC/enemy definitions
│   │   └── skeleton.json
│   ├── 📂 Dialogues/             # Branching dialogue trees
│   │   ├── flowey_intro.json
│   │   └── guide_hint.json
│   └── 📂 Textures/              # PNG sprite assets
│       ├── 📂 Tiles/             # floor_stone, wall_brick, water, etc.
│       ├── 📂 NPCs/              # flowey, skeleton, guard, etc.
│       ├── 📂 Battle/            # player_heart, bullets, etc.
│       └── 📂 UI/                # hp_bar, buttons, etc.
│
├── 📂 Source/                    # Core engine code (max 300 lines/file)
│   │
│   ├── 📂 Core/                  # Engine core
│   │   ├── engine.py             # Main game loop (229 lines)
│   │   ├── state_manager.py      # Scene stack management
│   │   ├── content_loader.py     # JSON content loading
│   │   └── save_manager.py       # Save/load system
│   │
│   ├── 📂 Terminal/              # 🆕 Custom Terminal Engine
│   │   ├── adapter.py            # GPU ↔ Terminal bridge (293 lines)
│   │   ├── context.py            # Headless OpenGL context (194 lines)
│   │   ├── input.py              # Native input handling (247 lines)
│   │   └── __init__.py
│   │
│   ├── 📂 Shaders/               # 🆕 GLSL Shader System
│   │   ├── base.vert             # Vertex shader
│   │   ├── base.frag             # Base fragment shader
│   │   ├── light.frag            # Point light + ambient
│   │   ├── glow.frag             # Bloom/glow effect
│   │   ├── water.frag            # Water wave distortion
│   │   └── __init__.py           # Shader loader
│   │
│   ├── 📂 Effects/               # 🆕 GPU Effects System
│   │   ├── manager.py            # Effects coordinator (257 lines)
│   │   ├── lights.py             # Light system (171 lines)
│   │   ├── particles.py          # Particle system (244 lines)
│   │   └── __init__.py
│   │
│   ├── 📂 Rendering/             # Rendering pipeline
│   │   ├── layer_manager.py      # Layer composition (259 lines)
│   │   ├── sprite_loader.py      # PNG sprite loading
│   │   ├── terminal_detect.py    # Terminal capability detection
│   │   └── viewport.py           # Camera/viewport
│   │
│   ├── 📂 Logic/                 # Game logic
│   │   ├── battle_system.py      # Turn-based combat
│   │   ├── battle_entities.py    # Combat entities
│   │   └── dialogue_engine.py    # Dialogue processing
│   │
│   ├── 📂 Scenes/                # Game scenes
│   │   ├── base_scene.py         # Scene interface
│   │   ├── menu_scene.py         # Main menu
│   │   ├── map_scene.py          # Exploration
│   │   ├── battle_scene.py       # Combat
│   │   └── dialogue_scene.py     # Dialogue display
│   │
│   ├── 📂 Entities/              # Game entities
│   │   ├── entity.py             # Base entity
│   │   ├── player.py             # Player controller
│   │   └── npc.py                # NPC behavior
│   │
│   └── 📂 Physics/               # Physics system
│       ├── collision.py          # Grid collision
│       └── movement.py           # Movement processing
│
├── 📂 MCP/                       # AI Integration
│   ├── server.py                 # MCP server (705 lines)
│   ├── tool_defs.py              # 24 tool definitions
│   └── mcp_config.json           # MCP configuration
│
├── 📂 Editor/                    # Terminal map editor
│   └── map_editor.py
│
├── 📂 Tools/                     # CLI utilities
│   ├── cli.py                    # Command-line interface
│   └── create_sprites.py         # Sprite generator
│
├── main.py                       # Entry point
├── requirements.txt              # Dependencies
└── README.md                     # This file
```

---

## 🚀 Quick Start

### Installation

```bash
# Clone repository
git clone https://github.com/yourusername/ALT_LAS_ENGINE.git
cd ALT_LAS_ENGINE

# Create virtual environment
python -m venv venv
source venv/bin/activate  # Linux/Mac
# or: venv\Scripts\activate  # Windows

# Install dependencies
pip install -r requirements.txt

# Run the game
python main.py
```

### Requirements

| Requirement | Version | Purpose |
|-------------|---------|---------|
| Python | 3.10+ | Runtime |
| Pillow | 10.0+ | Sprite loading |
| ModernGL | 5.8+ | GPU shaders (optional) |
| glcontext | 2.3+ | Headless GL (optional) |

---

## 🎨 Render Pipeline

```
┌─────────────────────────────────────────────────────────────────┐
│                    3-LEVEL FALLBACK SYSTEM                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │ LEVEL 1: GPU Shader (ModernGL + GLSL)                       │ │
│  │ ✓ Point lights (max 8) + Ambient lighting                   │ │
│  │ ✓ Glow/Bloom post-processing                                │ │
│  │ ✓ Water wave distortion                                     │ │
│  │ ✓ Output: Kitty/Sixel protocol                              │ │
│  │ ⚡ Requirements: OpenGL 3.3+, 128MB VRAM                    │ │
│  └─────────────────────────────────────────────────────────────┘ │
│                           ↓ Fallback                              │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │ LEVEL 2: Pre-render (Pillow CPU)                            │ │
│  │ ✓ CPU-based lighting effects                                │ │
│  │ ✓ Basic sprite compositing                                  │ │
│  │ ✓ Output: Kitty/Sixel protocol                              │ │
│  └─────────────────────────────────────────────────────────────┘ │
│                           ↓ Fallback                              │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │ LEVEL 3: ASCII (ANSI TrueColor)                             │ │
│  │ ✓ Character-based rendering                                 │ │
│  │ ✓ 24-bit color support                                      │ │
│  │ ✓ Works on ALL terminals                                    │ │
│  └─────────────────────────────────────────────────────────────┘ │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 📊 Technical Specifications

| Specification | Value |
|--------------|-------|
| **Resolution** | 960×544 (16:9) |
| **Grid Size** | 60×34 tiles |
| **Tile Size** | 16×16 pixels |
| **Target FPS** | 60 FPS |
| **Min FPS** | 30 FPS |
| **Shader Level** | Level 2 (Lighting + Effects) |
| **Max Lights** | 8 point lights |
| **Max Particles** | 500 |
| **GPU API** | OpenGL 3.3+ |
| **Min VRAM** | 128 MB |

### Sprite Standards

| Type | Size |
|------|------|
| Tiles | 16×16 px |
| Player/NPC | 16×24 px |
| Bosses | 48×64 px |
| UI Elements | Flexible |

---

## 🎮 Controls

| Key | Action |
|-----|--------|
| `↑↓←→` / `WASD` | Movement |
| `Z` / `Enter` | Interact / Confirm |
| `X` | Cancel |
| `Escape` | Menu |
| `I` | Inventory |
| `F5` | Quick Save |
| `F9` | Quick Load |
| `F1` | Debug Overlay |

---

## 🤖 MCP Tools (24 Available)

### Map Management
| Tool | Description |
|------|-------------|
| `create_map` | Create new map with dimensions |
| `add_npc` | Add NPC to map |
| `add_trigger` | Add trigger zone |
| `set_tile` | Modify individual tiles |
| `validate_map` | Check for errors |
| `get_map` | Get map data |
| `remove_npc` | Remove NPC from map |

### Sprite Management
| Tool | Description |
|------|-------------|
| `set_tile_sprite` | Assign sprite to tile type |
| `set_npc_sprite` | Assign sprite to NPC |
| `list_sprites` | List all available sprites |
| `validate_sprite` | Check sprite format |
| `create_sprite_placeholder` | Generate placeholder PNG |

### Shader & Effects
| Tool | Description |
|------|-------------|
| `set_ambient_light` | Set global ambient light |
| `add_point_light` | Add dynamic point light |
| `remove_point_light` | Remove point light |
| `list_lights` | List all active lights |
| `set_glow_effect` | Enable/disable bloom |
| `set_water_effect` | Enable/disable water waves |
| `spawn_particles` | Create particle effects |
| `get_render_info` | Get GPU/terminal capabilities |
| `set_shader_quality` | Set quality level |

### Content Management
| Tool | Description |
|------|-------------|
| `create_dialogue` | Create dialogue tree |
| `create_character` | Create enemy definition |
| `list_content` | List all content |

---

## 📝 Content Examples

### Creating a Map

```json
{
  "name": "dungeon_entrance",
  "width": 30,
  "height": 20,
  "player_spawn": {"x": 15, "y": 18},
  "tiles": [[1,1,1,...], ...],
  "tile_sprites": {
    "0": "Tiles/floor_stone.png",
    "1": "Tiles/wall_brick.png",
    "2": "Tiles/water.png"
  },
  "triggers": [
    {"x": 15, "y": 0, "event": "map:dungeon_hall"}
  ],
  "npcs": [
    {
      "name": "gatekeeper",
      "char": "G",
      "sprite": "NPCs/guard.png",
      "x": 15, "y": 10,
      "dialogue_id": "gatekeeper_intro"
    }
  ]
}
```

### Creating Dialogue

```json
{
  "id": "gatekeeper_intro",
  "nodes": {
    "start": {
      "speaker": "Gatekeeper",
      "text": "Halt! None may pass without the seal.",
      "choices": [
        {"text": "I have the seal", "next": "has_seal", "condition": "flag:seal_obtained"},
        {"text": "Where can I find it?", "next": "hint"},
        {"text": "I'll find another way", "next": "leave"}
      ]
    },
    "has_seal": {
      "speaker": "Gatekeeper",
      "text": "Very well. You may pass.",
      "action": "trigger:open_gate"
    }
  }
}
```

### Creating an Enemy

```json
{
  "name": "Skeleton",
  "hp": 20,
  "attack": 4,
  "defense": 1,
  "sprite": "NPCs/skeleton.png",
  "battle_patterns": ["rain", "bones"]
}
```

---

## 📈 Current Status (v0.3.0-alpha)

### ✅ Completed

| Component | Status | Description |
|-----------|--------|-------------|
| Core Engine | ✅ Complete | Game loop, state management, content loading |
| Terminal Module | ✅ Complete | GPU adapter, headless GL, native input |
| Shader System | ✅ Complete | Light, glow, water shaders |
| Effects System | ✅ Complete | Lights, particles, screen shake |
| Rendering | ✅ Complete | 4-layer system with 3-level fallback |
| Battle System | ✅ Complete | Turn-based + bullet hell dodge |
| Dialogue Engine | ✅ Complete | Branching dialogues with conditions |
| MCP Server | ✅ Complete | 24 AI management tools |
| Map Editor | ✅ Complete | Terminal-based editor |

### 🚧 In Progress

| Component | Status | Description |
|-----------|--------|-------------|
| Sprite Batching | 🚧 60% | GPU sprite batching for performance |
| Sound System | 🚧 30% | Terminal audio via beep/PCM |
| Save System | 🚧 50% | JSON save/load with slots |

### 📋 Planned (v0.4.0)

| Component | Priority | Description |
|-----------|----------|-------------|
| Normal Maps | High | Level 3 shader upgrade |
| Shadow System | Medium | Dynamic shadows |
| Animation System | High | Sprite animations |
| Inventory UI | Medium | Item management |
| Quest System | Medium | Objective tracking |

---

## 🗺️ Roadmap

### Phase 1: Foundation (v0.1-0.3) ✅
- [x] Core engine architecture
- [x] Terminal rendering with fallback
- [x] GPU shader system
- [x] Battle system
- [x] Dialogue engine
- [x] MCP integration

### Phase 2: Polish (v0.4-0.6) 🚧
- [ ] Sprite batching optimization
- [ ] Animation system
- [ ] Sound/music support
- [ ] Inventory system
- [ ] Quest system
- [ ] Save slots

### Phase 3: Advanced (v0.7-0.9) 📋
- [ ] Normal map support
- [ ] Dynamic shadows
- [ ] Particle editor
- [ ] Visual scripting
- [ ] Plugin system
- [ ] Web target (Pyodide)

### Phase 4: Release (v1.0) 🎯
- [ ] Complete demo game
- [ ] Documentation site
- [ ] Tutorial series
- [ ] Asset pack
- [ ] Steam release

---

## 🏗️ Design Principles

1. **300 Lines Max** - No file exceeds 300 lines for readability
2. **Data-Logic Separation** - Content in JSON, not code
3. **Always Fallback** - ASCII mode works everywhere
4. **Adapter Pattern** - New features don't break existing code
5. **AI-First Design** - MCP tools enable AI content creation
6. **Future-Proof** - Modular architecture for easy upgrades
7. **MIT License** - Commercial use allowed

---

## 📜 License

| Component | License |
|-----------|---------|
| Engine Code | MIT |
| ModernGL | MIT |
| Pillow | PIL License |
| Generated Content | Project-owned |

---

## 🤝 Contributing

1. Fork the repository
2. Create feature branch (`git checkout -b feature/amazing-feature`)
3. Commit changes (`git commit -m 'Add amazing feature'`)
4. Push to branch (`git push origin feature/amazing-feature`)
5. Open Pull Request

### Code Standards
- Max 300 lines per file
- PEP 8 formatting
- Type hints recommended
- Docstrings required

---

## 📞 Contact

- **Issues**: [GitHub Issues](https://github.com/yourusername/ALT_LAS_ENGINE/issues)
- **Discussions**: [GitHub Discussions](https://github.com/yourusername/ALT_LAS_ENGINE/discussions)

---

<p align="center">
  <i>Built with ❤️ for terminal gaming enthusiasts</i>
</p>
