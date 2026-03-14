# 🎨 GFX-001: Normal Mapping Plan

**Görev ID:** GFX-001
**Referans:** Octopath Traveler
**Durum:** 📋 ONAY BEKLİYOR
**Sorumlu:** Kemal
**Oluşturulma:** 14 Mart 2025

---

## 📋 Görev Özeti

2D sprite'larda normal map kullanarak dinamik ışıklandırma efekti oluşturmak.

---

## 🎯 Hedefler

| Hedef | Açıklama |
|-------|----------|
| Dinamik aydınlatma | Işık kaynağı hareket ettikçe sprite'ta yansısın |
| Depth efekti | Sprite'larda 3D derinlik hissi |
| Performans | 60 FPS korunsun |
| Kolay kullanım | Artist'ler kolayca normal map ekleyebilsin |

---

## 📁 Dosya Yapısı

```
src/render/
├── normal_map.vert       # Vertex shader
├── normal_map.frag       # Fragment shader
├── normal_mapper.py      # Normal map generator
└── normal_renderer.py    # Render pipeline

assets/
├── normal_maps/          # Oluşturulan normal map'ler
│   ├── player_normal.png
│   └── tiles/
└── sprites/              # Orijinal sprite'lar

tools/
└── generate_normals.py   # CLI tool
```

---

## 🔧 Teknik Uygulama

### 1. Normal Map Vertex Shader

```glsl
// normal_map.vert
#version 330

in vec2 in_pos;
in vec2 in_tex;
out vec2 v_tex;
out vec2 v_world_pos;

uniform vec2 u_offset;
uniform vec2 u_screen;
uniform vec2 u_light_pos;

void main() {
    vec2 pos = (in_pos + u_offset) / u_screen * 2.0 - 1.0;
    gl_Position = vec4(pos.x, -pos.y, 0.0, 1.0);
    v_tex = in_tex;
    v_world_pos = in_pos + u_offset;
}
```

### 2. Normal Map Fragment Shader

```glsl
// normal_map.frag
#version 330

in vec2 v_tex;
in vec2 v_world_pos;
out vec4 fragColor;

uniform sampler2D u_texture;
uniform sampler2D u_normal_map;
uniform vec2 u_light_pos;
uniform vec3 u_light_color;
uniform float u_light_intensity;
uniform float u_ambient;

void main() {
    vec4 color = texture(u_texture, v_tex);
    if (color.a < 0.1) discard;
    
    // Normal from map (0-1 → -1 to 1)
    vec3 normal = texture(u_normal_map, v_tex).rgb;
    normal = normalize(normal * 2.0 - 1.0);
    
    // Light direction
    vec2 light_dir = normalize(u_light_pos - v_world_pos);
    
    // Simple 2D lighting
    float diff = max(dot(vec3(light_dir, 0.5), normal), 0.0);
    
    // Combine
    vec3 final = color.rgb * (u_ambient + diff * u_light_intensity * u_light_color);
    
    fragColor = vec4(final, color.a);
}
```

### 3. Normal Map Generator (Python)

```python
# src/render/normal_mapper.py
"""
Height map'ten normal map üreteci.
Sobel filter kullanarak gradient hesaplar.
"""

import numpy as np
from PIL import Image

class NormalMapper:
    """Convert height/depth maps to normal maps."""
    
    @staticmethod
    def from_heightmap(heightmap: Image.Image, strength: float = 1.0) -> Image.Image:
        """
        Convert a height map to a normal map.
        
        Args:
            heightmap: Grayscale PIL Image
            strength: Normal strength (default 1.0)
        
        Returns:
            RGB normal map as PIL Image
        """
        # Convert to numpy array
        arr = np.array(heightmap.convert('L'), dtype=np.float32) / 255.0
        
        # Sobel kernels
        sobel_x = np.array([[-1, 0, 1], [-2, 0, 2], [-1, 0, 1]])
        sobel_y = np.array([[-1, -2, -1], [0, 0, 0], [1, 2, 1]])
        
        # Apply Sobel
        from scipy import ndimage
        dx = ndimage.convolve(arr, sobel_x) * strength
        dy = ndimage.convolve(arr, sobel_y) * strength
        
        # Calculate normal (dz = 1)
        dz = np.ones_like(arr)
        length = np.sqrt(dx**2 + dy**2 + dz**2)
        
        # Normalize and convert to 0-255 range
        nx = (dx / length + 1) * 127.5
        ny = (dy / length + 1) * 127.5
        nz = (dz / length + 1) * 127.5
        
        # Create RGB image
        normal = np.stack([nx, ny, nz], axis=2).astype(np.uint8)
        
        return Image.fromarray(normal, 'RGB')
    
    @staticmethod
    def from_sprite(sprite: Image.Image, edge_detection: bool = True) -> Image.Image:
        """
        Generate normal map from sprite by treating edges as depth.
        
        Args:
            sprite: RGBA PIL Image
            edge_detection: Use alpha edge for depth
        
        Returns:
            RGB normal map as PIL Image
        """
        # Use alpha channel as height
        alpha = sprite.split()[-1]
        
        # Invert: opaque = high, transparent = low
        height = Image.eval(alpha, lambda x: 255 - x)
        
        return NormalMapper.from_heightmap(height)
```

---

## 📊 Alt Görevler

| ID | Görev | Tahmini Süre | Durum |
|----|-------|--------------|-------|
| GFX-001-1 | Vertex shader yaz | 1 saat | ⬜ |
| GFX-001-2 | Fragment shader yaz | 2 saat | ⬜ |
| GFX-001-3 | Normal mapper class | 3 saat | ⬜ |
| GFX-001-4 | CLI tool | 1 saat | ⬜ |
| GFX-001-5 | Demo entegrasyonu | 2 saat | ⬜ |
| GFX-001-6 | Test & debug | 2 saat | ⬜ |

**Toplam:** ~11 saat

---

## 🧪 Test Senaryoları

| Test | Açıklama | Beklenen Sonuç |
|------|----------|----------------|
| T1 | Player sprite + moving light | Işık hareketle sprite değişsin |
| T2 | Tile + static light | Tutarlı aydınlatma |
| T3 | Multiple lights | Doğru blending |
| T4 | No normal map | Fallback to regular render |
| T5 | Performance | 60 FPS stable |

---

## 📐 API Tasarımı

```python
# Kullanım örneği
from src.render.normal_renderer import NormalRenderer

# Renderer oluştur
normal_renderer = NormalRenderer(ctx)

# Normal map yükle
normal_renderer.load_normal_map("player", "assets/normal_maps/player_normal.png")

# Render
normal_renderer.render(
    sprite=player_sprite,
    normal_map="player",
    position=(100, 100),
    lights=[{"pos": (200, 150), "color": (1, 1, 0.8), "intensity": 1.0}]
)
```

---

## ⚠️ Riskler ve Çözümler

| Risk | Etki | Çözüm |
|------|------|-------|
| Performans düşüşü | Yüksek | Batch rendering, instancing |
| Normal map kalitesi | Orta | Farklı generator algoritmaları |
| GPU uyumluluk | Düşük | Shader version fallback |

---

## 🔗 Bağımlılıklar

- [x] ModernGL context
- [x] PIL/Pillow
- [x] NumPy
- [ ] SciPy (normal map generation için)

---

## ✅ Onay Gereken Noktalar

1. Normal map storage format (PNG RGB?)
2. Light limit (max 8 lights?)
3. Fallback behavior (no normal map?)
4. Tool integration (CLI vs GUI?)

---

## 📝 Notlar

- Octopath Traveler edge-detection tekniği kullanılıyor
- Soft normals için blur eklenebilir
- Daha iyi sonuçlar için artist tarafından elle düzeltilebilir

---

**Plan Durumu:** ✅ ONAYLANDI (14 Mart 2025 - 17:45)

*Bu plan Kemal tarafından hazırlanmıştır.*
