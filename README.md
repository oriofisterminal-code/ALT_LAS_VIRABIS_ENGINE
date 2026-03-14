# ALT_LAS Engine

> Python game engine with GPU shaders, Undertale-style combat, and AI integration

![Python](https://img.shields.io/badge/Python-3.10+-blue.svg)
![License](https://img.shields.io/badge/License-MIT-green.svg)
![Docker](https://img.shields.io/badge/Docker-Ready-blue.svg)
![Window](https://img.shields.io/badge/Mode-Window%20%7C%20Terminal-purple.svg)

## Features

- **Real Window** - SDL2/GLFW with OpenGL rendering
- **GPU Shaders** - GLSL lighting, glow, water effects
- **Turn-based Combat** - FIGHT/ACT/ITEM/MERCY system
- **Bullet Hell Dodge** - Undertale-style soul system
- **AI Integration** - MCP (26 tools) + HTTP API (51 handlers)
- **Dual Mode** - Window (default) or Terminal (for AI)

---

## Quick Start

### Window Mode (Recommended)

```bash
# Clone
git clone https://github.com/evpozipo-arch/ALT_LAS_ENGINE.git
cd ALT_LAS_ENGINE

# Install
pip install -r requirements.txt

# Run
python main_window.py
```

### Terminal Mode (For AI/MCP)

```bash
python main.py
```

### Docker

```bash
docker-compose run game
```

---

## Project Structure

```
ALT_LAS_ENGINE/
├── main_window.py          # Window mode entry (default)
├── main.py                 # Terminal mode entry
│
├── src/                    # Source Code
│   ├── core/               # Engine, State, Save, Loader
│   ├── game/               # Scenes, Battle, Entities
│   ├── render/             # Shaders, Effects, Lights
│   ├── window/             # SDL2/GLFW Window System
│   ├── terminal/           # Terminal I/O (for AI)
│   └── api/                # HTTP REST API (51 handlers)
│
├── mcp/                    # AI Integration (26 tools)
├── assets/                 # Game Content
│   ├── textures/           # PNG sprites
│   ├── maps/               # JSON maps
│   ├── dialogues/          # JSON dialogues
│   └── characters/         # JSON entities
│
├── config/                 # Settings
├── tools/                  # CLI Utilities
└── docs/                   # Documentation
```

---

## Controls

| Key | Action |
|-----|--------|
| WASD / Arrows | Move |
| Z / Enter | Confirm |
| X | Cancel |
| ESC | Menu / Exit |
| F1 | Debug Mode |

---

## AI Integration

### MCP Tools (26)

| Category | Tools |
|----------|-------|
| Maps | `create_map`, `get_map`, `set_tile`, `validate_map` |
| NPCs | `add_npc`, `remove_npc`, `set_npc_sprite` |
| Sprites | `set_tile_sprite`, `list_sprites`, `create_sprite_placeholder` |
| Effects | `set_ambient_light`, `add_point_light`, `spawn_particles` |
| Content | `create_dialogue`, `create_character` |
| System | `list_content`, `get_render_info`, `set_shader_quality` |

### HTTP REST API

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/command` | POST | Execute single command |
| `/api/batch` | POST | Execute multiple commands |
| `/api/state` | GET | Get engine status |
| `/api/health` | GET | Health check |

---

## Requirements

| Package | Version |
|---------|---------|
| Python | 3.10+ |
| Pillow | 10.0+ |
| ModernGL | 5.8+ |
| moderngl-window | 2.4+ |

---

## Documentation

- [ROADMAP.md](docs/ROADMAP.md) - Future plans
- [shaders.md](docs/shaders.md) - Shader documentation
- [renderer.md](docs/renderer.md) - Render pipeline

---

## License

MIT License - Commercial use allowed
