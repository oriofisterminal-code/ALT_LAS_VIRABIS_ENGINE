# ALT_LAS Engine

> Terminal-based game engine with GPU shaders and Undertale-style combat

![Python](https://img.shields.io/badge/Python-3.10+-blue.svg)
![License](https://img.shields.io/badge/License-MIT-green.svg)
![Docker](https://img.shields.io/badge/Docker-Ready-blue.svg)

## Features

- **GPU Shaders** - GLSL lighting, glow, water effects
- **3-Level Fallback** - GPU → CPU → ASCII
- **Turn-based Combat** - FIGHT/ACT/ITEM/MERCY
- **Bullet Hell Dodge** - Undertale-style soul system
- **MCP Integration** - 24 AI management tools

## Structure

```
ALT_LAS_ENGINE/
├── src/                    # Source Code
│   ├── core/               # Engine core (5 files)
│   ├── game/               # Game logic (14 files)
│   ├── render/             # Rendering + Shaders (16 files)
│   └── terminal/           # Terminal I/O (3 files)
│
├── assets/                 # Game Content
│   ├── textures/           # PNG sprites (21 files)
│   ├── maps/               # JSON maps
│   ├── dialogues/          # JSON dialogues
│   └── characters/         # JSON entities
│
├── config/                 # Settings
├── mcp/                    # AI Integration
├── tools/                  # CLI Utilities
└── docs/                   # Documentation
```

---

## 🚀 Quick Start

### Option 1: Docker (Recommended)

```bash
# Clone
git clone https://github.com/evpozipo-arch/ALT_LAS_ENGINE.git
cd ALT_LAS_ENGINE

# Run with Docker
docker-compose run game

# Or build and run manually
docker build -t alt_las_engine .
docker run -it --rm alt_las_engine
```

### Option 2: Local Installation

```bash
# Clone
git clone https://github.com/evpozipo-arch/ALT_LAS_ENGINE.git
cd ALT_LAS_ENGINE

# Setup
python -m venv venv
source venv/bin/activate
pip install -r requirements.txt

# Run
python main.py
```

---

## 🐳 Docker Commands

| Command | Description |
|---------|-------------|
| `docker-compose run game` | Run main game |
| `docker-compose up mcp` | Start MCP server |
| `docker-compose run dev` | Development shell |
| `docker-compose run editor` | Map editor |
| `docker-compose build` | Rebuild images |
| `docker-compose down` | Stop all services |

### Docker with GPU Support

```bash
# NVIDIA GPU
docker-compose run --gpu all game

# With X11 forwarding (Linux)
xhost +local:docker
docker-compose run game
```

---

## Requirements

| Package | Version |
|---------|---------|
| Python | 3.10+ |
| Pillow | 10.0+ |
| ModernGL | 5.8+ |

---

## Controls

| Key | Action |
|-----|--------|
| WASD / Arrows | Move |
| Z / Enter | Confirm |
| X | Cancel |
| ESC | Menu |

---

## MCP Tools (24)

| Category | Tools |
|----------|-------|
| Maps | create_map, add_npc, set_tile, validate_map |
| Sprites | set_sprite, list_sprites, create_placeholder |
| Effects | add_light, set_glow, spawn_particles |
| Content | create_dialogue, create_character |

---

## License

MIT License - Commercial use allowed
