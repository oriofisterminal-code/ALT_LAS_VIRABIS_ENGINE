# ALT_LAS Engine - AI Onboarding

> Bu belge, projeye yeni başlayan AI'lar için hazırlanmıştır. 
> Önceki sohbetin özetini ve devam edilecek işleri içerir.

---

## 🎯 Proje Özeti

**ALT_LAS Engine**, Python ile yazılmış bir 2D oyun motorudur. 
Undertale tarzı savaş sistemi, GPU shader'ları ve AI entegrasyonu sunar.

### Özellikleri
- **Dual Mode:** Window (SDL2/GLFW) + Terminal (AI/MCP için)
- **GPU Rendering:** ModernGL ile GLSL shader'ları
- **AI Integration:** 26 MCP tool + 51 HTTP API handler
- **Battle System:** FIGHT/ACT/ITEM/MERCY mekanikleri

---

## 📁 Proje Yapısı

```
ALT_LAS_ENGINE/
├── main_window.py          # Window mode (varsayılan)
├── main.py                 # Terminal mode (AI için)
│
├── src/
│   ├── core/               # Engine, State, Save, Loader
│   ├── game/               # Scenes, Battle, Entities, Demo
│   ├── render/             # Shaders, Effects, Lights, Particles
│   ├── window/             # SDL2/GLFW Window System
│   ├── terminal/           # Terminal I/O (MCP stdin)
│   └── api/                # HTTP REST API (51 handlers)
│
├── mcp/                    # AI MCP Server (26 tools)
├── assets/
│   ├── textures/           # PNG sprites
│   ├── maps/               # JSON maps
│   ├── dialogues/          # JSON dialogues
│   └── characters/         # JSON entities
│
├── config/                 # settings.json, keys.json
├── docs/                   # ROADMAP, shaders, etc.
└── tools/                  # CLI utilities
```

---

## 🕐 Son Yapılanlar (v0.3.1) ✅

| Tarih | İşlem | Durum |
|-------|-------|-------|
| 14.03.2025 | LogLevel Enum (6 seviye) | ✅ |
| 14.03.2025 | ErrorCode Enum (E001-E399) | ✅ |
| 14.03.2025 | LogManager sınıfı (Console + File) | ✅ |
| 14.03.2025 | LoadingScreen (Progress + ASCII + Tips) | ✅ |
| 14.03.2025 | SecurityMasker (IP, API key maskeleme) | ✅ |
| 14.03.2025 | Engine entegrasyonu | ✅ |
| 14.03.2025 | ROADMAP.md güncellendi | ✅ |

### GitHub Durumu
```
Repository: evpozipo-arch/ALT_LAS_ENGINE
Branch: master (devin/1773441551-alt-las-engine merged)
Son Commit: 917be5d (Merge PR #4)
Current Version: v0.3.1
```

---

## 🔄 Sonraki Adım (v0.4.0)

### AI Code Editor Tools

**Planlanan MCP Tools:**
| Tool | Açıklama |
|------|----------|
| `read_file` | Proje dosyasını oku |
| `write_file` | Yeni dosya oluştur |
| `edit_file` | Dosya düzenle |
| `run_python` | Python çalıştır |
| `git_commit` | Git commit |

---

## 🚀 Çalıştırma

```bash
# Window mode (önerilen, grafik testi)
python main_window.py

# Terminal mode (AI/MCP için)
python main.py

# Fullscreen demo
python main_window.py --fullscreen

# Debug mode
python main_window.py --debug
```

### Gereksinimler
```
Python 3.10+
Pillow 10.0+
ModernGL 5.8+
moderngl-window 2.4+
```

---

## 🤖 AI Sistemi

### Mevcut MCP Tools (26)

| Kategori | Tools |
|----------|-------|
| Maps | `create_map`, `get_map`, `set_tile`, `validate_map` |
| NPCs | `add_npc`, `remove_npc`, `set_npc_sprite` |
| Sprites | `set_tile_sprite`, `list_sprites`, `create_sprite_placeholder` |
| Effects | `set_ambient_light`, `add_point_light`, `spawn_particles` |
| Content | `create_dialogue`, `create_character` |
| System | `list_content`, `get_render_info`, `set_shader_quality` |

### HTTP REST API (51 handlers)

| Endpoint | Açıklama |
|----------|----------|
| POST /api/command | Tek komut çalıştır |
| POST /api/batch | Çoklu komut |
| GET /api/state | Motor durumu |
| GET /api/health | Sağlık kontrolü |

### v0.4.0 için Planlanan AI Tools

| Tool | Açıklama |
|------|----------|
| `read_file` | Proje dosyasını oku |
| `write_file` | Yeni dosya oluştur |
| `edit_file` | Dosya düzenle |
| `run_python` | Python çalıştır |
| `git_commit` | Git commit |

---

## ⚠️ Önemli Notlar

### Terminal Mode Neden Kaldı?
**KALMADI!** Terminal mode AI/MCP sistemi için **korundu**.
- MCP Server stdin/stdout kullanıyor
- Window mode oyun için varsayılan
- Terminal mode AI yönetimi için

### Source/ Klasörü
**SİLİNDİ!** Boştu, tüm kod `src/` altında.

### Import Yolları
```python
# DOĞRU
from src.core.engine import GameEngine
from src.render.layers import get_layer_manager
from mcp.tools import TOOLS

# YANLIŞ (eski, Source/ klasörü yok artık)
from Source.Core.content_loader import ContentLoader
```

---

## 📋 Sonraki Adımlar (v0.4.0)

1. **AI Code Editor Tools** - MCP tools ile tam proje kontrolü
2. **read_file/write_file** - Dosya okuma/yazma MCP tools
3. **run_python** - Python kod çalıştırma tool'u
4. **git_commit** - Git entegrasyonu
5. **search_code** - Codebase arama tool'u

---

## 🔗 Hızlı Referans

| Dosya | Açıklama |
|-------|----------|
| `src/core/engine.py` | Ana motor, dual mode |
| `src/core/logging_system.py` | Logging & Debug System (v0.3.1) |
| `src/window/manager.py` | SDL2/GLFW window |
| `src/game/demo_scene.py` | Grafik test sahnesi |
| `src/render/adapter.py` | GPU render adapter |
| `mcp/server.py` | AI MCP sunucusu |
| `assets/maps/demo_level.json` | Test haritası |
| `docs/ROADMAP.md` | Yol haritası |

---

## 💡 İpuçları

1. **Yeni sahne eklemek için:** `src/game/` altında `BaseScene` extend et
2. **API eklemek için:** `src/api/handlers/` altında yeni handler oluştur
3. **Shader yazmak için:** `src/render/*.frag` veya `.vert` dosyaları
4. **Harita oluşturmak için:** MCP `create_map` tool'u veya JSON

---

## 📞 İletişim Noktaları

- **GitHub:** evpozipo-arch/ALT_LAS_ENGINE
- **Branch:** devin/1773441551-alt-las-engine
- **Son Commit:** 195b932

---

*Bu belge 14 Mart 2025'te oluşturuldu. 
Bir AI olarak bu projeye devam ediyorsanız, yukarıdaki bilgiler sizi hızlandıracaktır.*

**Hoş geldiniz! 🎮**
