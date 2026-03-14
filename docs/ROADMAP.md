# ALT_LAS Engine - Roadmap

## Version History

| Version | Date | Description |
|---------|------|-------------|
| v0.1.0 | Initial | Basic terminal rendering |
| v0.2.0 | Atlas_001 | GPU shaders, battle system |
| v0.3.0 | Current | Dual mode (Terminal + Window) |
| v0.3.1 | Next | Logging & Debug System |

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
- [x] Demo Level (graphics test)
- [x] Project cleanup (Source/ removed)

---

## v0.3.1 - Logging & Debug System (NEXT)

### Toplantı Kararları (14.03.2025)

**Katılımcılar:**
| Uzman | Yardımcı Meslek | Karakter | Sorumlu Alan |
|-------|-----------------|----------|--------------|
| Dr. Elif - AI İletişim | Sosyolog | Çok konuşan, cimri | Toplantı yönetimi |
| Mehmet - Backend | Muhasebeci | Titiz, kontrol manyağı | Log seviyeleri, maliyet |
| Kemal - GPU | İnşaat Mühendisi | Sabırsız, işini sever | Loading ekranı, progress |
| Selin - UX | Psikolog | Mükemmeliyetçi | Tips, renkler, UX |
| Cem - DevOps | Aşçı | Stresli, organize | Log manager tarifi |
| Ayşe - DB | Kütüpaneci | Ketum, detaycı | Dosya arşivleme |
| Burak - Güvenlik | Dedektif | Şüpheci | Maskeleme, güvenlik |
| Deniz - Frontend | Grafik Tasarımcı | Yaratıcı | ASCII art, renkler |
| Feyza - Test | Kalite Kontrol | Eleştirel | Hata kodları |
| Prof. Ali - Analist | Felsefeci | Derin düşünür | Mimari, felsefe |

### Implementasyon Planı

```
src/core/logging_system.py
├── LogLevel (Enum)
│   ├── DEBUG    → Geliştirici detayları
│   ├── INFO     → Normal işlemler
│   ├── SUCCESS  → Başarılı işlemler (yeşil)
│   ├── WARNING  → Dikkat gerekli
│   ├── ERROR    → Hata oluştu
│   └── CRITICAL → Sistem çöküyor
│
├── ErrorCode (Enum)
│   ├── E001-E099 → GPU hataları
│   ├── E100-E199 → Render hataları
│   ├── E200-E299 → Dosya hataları
│   └── E300-E399 → Sistem hataları
│
├── LogManager
│   ├── Console output (renkli + emoji)
│   ├── File logging (rotasyonlu)
│   ├── Security masking
│   └── Callback sistemi
│
└── LoadingScreen
    ├── Progress bar
    ├── ASCII art
    ├── Tips sistemi
    └── Error handling
```

### Özellikler

| Özellik | Sorumlu | Açıklama |
|---------|---------|----------|
| Log Seviyeleri | Mehmet | DEBUG→CRITICAL arası 6 seviye |
| Hata Kodları | Feyza | E001-E399 kategorize kodlar |
| Renkli Console | Deniz | ANSI renkleri + emojiler |
| Dosya Arşivleme | Ayşe | 10MB'da rotasyon, 30 gün |
| Güvenlik Maskeleme | Burak | IP, API key, password |
| Loading Screen | Kemal + Selin | Progress bar + tips |
| ASCII Art | Deniz | ALT_LAS başlığı |

### Hata Kodları Listesi

```
GPU HATALARI (E001-E099):
├── E001: GPU_INIT_FAILED
├── E002: SHADER_COMPILE_ERROR
└── E003: OPENGL_CONTEXT_LOST

RENDER HATALARI (E100-E199):
├── E100: TEXTURE_LOAD_FAILED
├── E101: SPRITE_NOT_FOUND
└── E102: FRAME_BUFFER_ERROR

DOSYA HATALARI (E200-E299):
├── E200: FILE_NOT_FOUND
├── E201: PARSE_ERROR
└── E202: PERMISSION_DENIED

SİSTEM HATALARI (E300-E399):
├── E300: MEMORY_ALLOC_FAILED
├── E301: THREAD_ERROR
└── E302: TIMEOUT_ERROR
```

---

## v0.4.0 - AI Code Editor Tools

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

## v0.5.0 - Enhanced Graphics

- [ ] Sprite animation system
- [ ] Tile-based auto-tiling
- [ ] Parallax backgrounds
- [ ] Screen shake effects
- [ ] Transition animations

---

## v0.6.0 - Audio System

- [ ] BGM playback
- [ ] Sound effects
- [ ] Audio spatial positioning
- [ ] Volume control API

---

## v1.0.0 - Production Ready

- [ ] Full documentation
- [ ] Example games
- [ ] Tutorial system
- [ ] Performance optimization
- [ ] Windows/macOS/Linux builds

---

## Technical Debt

| Issue | Priority | Status |
|-------|----------|--------|
| Empty Source/ folder removed | Done | ✅ Completed |
| Import paths standardized | Done | ✅ Completed |
| Terminal mode kept for AI/MCP | Done | ✅ Completed |
| Logging system | High | 🔄 In Progress |

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
│  ┌──────────────────────────────────────────────┐   │
│  │          LOGGING & DEBUG SYSTEM               │   │
│  │  LogManager + LoadingScreen + ErrorCodes     │   │
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

*Last updated: 14 March 2025*
