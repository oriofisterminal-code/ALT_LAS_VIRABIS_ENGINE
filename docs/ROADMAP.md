# ALT_LAS Engine - Roadmap

## Version History

| Version | Date | Description |
|---------|------|-------------|
| v0.1.0 | Initial | Basic terminal rendering |
| v0.2.0 | Atlas_001 | GPU shaders, battle system |
| v0.3.0 | Current | Dual mode (Terminal + Window) |

---

## Current Status: v0.3.0

### Completed Features
- [x] GPU Shader Pipeline (GLSL)
- [x] Dual Mode Engine (Terminal + Window)
- [x] SDL2/GLFW Window Support
- [x] MCP Integration (26 tools)
- [x] HTTP REST API (51 handlers)
- [x] Undertale-style Battle System
- [x] Map/Dialogue/Character JSON System
- [x] Sprite Management
- [x] Light/Particle Effects

---

## Planned Features

### v0.4.0 - AI Code Editor Tools

**Goal:** Give AI full control over the entire project

| Tool | Description | Priority |
|------|-------------|----------|
| `read_file` | Read any file in project | High |
| `write_file` | Create new files | High |
| `edit_file` | Make targeted edits to existing files | High |
| `list_files` | Browse project structure | Medium |
| `delete_file` | Remove files | Medium |
| `run_python` | Execute Python code safely | High |
| `git_commit` | Commit changes to git | Medium |
| `search_code` | Search across codebase | Medium |

**Benefits:**
- AI can create new scenes
- AI can modify battle mechanics
- AI can add API handlers
- AI can write shaders
- AI can fix bugs autonomously

---

### v0.5.0 - Enhanced Graphics

- [ ] Sprite animation system
- [ ] Tile-based auto-tiling
- [ ] Parallax backgrounds
- [ ] Screen shake effects
- [ ] Transition animations

---

### v0.6.0 - Audio System

- [ ] BGM playback
- [ ] Sound effects
- [ ] Audio spatial positioning
- [ ] Volume control API

---

### v1.0.0 - Production Ready

- [ ] Full documentation
- [ ] Example games
- [ ] Tutorial system
- [ ] Performance optimization
- [ ] Windows/macOS/Linux builds

---

## Technical Debt

| Issue | Priority | Notes |
|-------|----------|-------|
| Empty Source/ folder removed | Done | Cleanup completed |
| Import paths standardized | Done | All use src/ now |
| Terminal mode optional | Medium | Keep for AI/MCP |

---

## Architecture

```
┌─────────────────────────────────────────────────────┐
│                   ALT_LAS ENGINE                     │
├─────────────────────────────────────────────────────┤
│                                                      │
│  ┌─────────────┐    ┌─────────────┐                 │
│  │   Window    │    │  Terminal   │ ← AI/MCP needs  │
│  │   Mode      │    │    Mode     │                 │
│  │ (SDL2/GLFW) │    │  (stdin)    │                 │
│  └──────┬──────┘    └──────┬──────┘                 │
│         │                  │                         │
│         └────────┬─────────┘                         │
│                  │                                   │
│         ┌────────▼────────┐                         │
│         │   Game Engine   │                         │
│         │   src/core/     │                         │
│         └────────┬────────┘                         │
│                  │                                   │
│    ┌─────────────┼─────────────┐                    │
│    │             │             │                    │
│  ┌─▼───┐   ┌─────▼─────┐   ┌───▼──┐               │
│  │ API │   │  Render   │   │ Game │               │
│  │51 H │   │  Pipeline │   │Scenes│               │
│  └─────┘   └───────────┘   └──────┘               │
│                                                      │
│  ┌──────────────────────────────────────────────┐   │
│  │              AI CONTROL LAYER                 │   │
│  │  MCP Server (26 tools) + HTTP API (51 H)     │   │
│  └──────────────────────────────────────────────┘   │
│                                                      │
└─────────────────────────────────────────────────────┘
```

---

## How to Contribute

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Run tests: `python -m pytest tests/`
5. Submit a pull request

---

*Last updated: March 2025*
