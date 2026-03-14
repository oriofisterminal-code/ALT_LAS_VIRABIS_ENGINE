# 🎨 GFX-002: Post-Processing (Bloom) Plan

**Görev ID:** GFX-002
**Referans:** Celeste
**Durum:** 📋 PLAN AŞAMASINDA
**Sorumlu:** Kemal (GPU), Deniz (Grafik), Selin (UX)
**Oluşturulma:** 14 Mart 2025

---

## 📋 Görev Özeti

Framebuffer tabanlı bloom/glow post-processing efekti oluşturmak. Parlak bölgeleri ayıklayıp blur ile yumuşatarak orijinal görüntüyle birleştirmek.

---

## 🎯 Hedefler

| Hedef | Açıklama |
|-------|----------|
| Bloom efekti | Parlak nesneler etrafında glow |
| Performans | 60 FPS korunsun |
| Ayarlanabilir | Intensity ve threshold kontrolleri |
| Uyumluluk | GFX-001 ile birlikte çalışsın |

---

## 📁 Dosya Yapısı

```
src/render/
├── framebuffer.py       # FBO yönetim sınıfı
├── post_process.py      # Post-process pipeline manager
├── bright_extract.frag  # Brightness extraction shader
├── blur.frag            # Gaussian blur shader
├── bloom.frag           # Bloom composite shader
└── post_vertex.vert     # Ortak vertex shader (fullscreen quad)

config/
└── post_process.json    # Bloom ayarları
```

---

## 🔧 Teknik Uygulama

### 1. Framebuffer Sınıfı

```python
# src/render/framebuffer.py
class Framebuffer:
    """
    ModernGL Framebuffer Object wrapper.
    
    Features:
    - Multiple color attachments
    - Depth/stencil support
    - Resize handling
    - Texture management
    """
    
    def __init__(self, ctx, width, height, attachments=1, has_depth=False):
        self.ctx = ctx
        self.width = width
        self.height = height
        self.fbo = None
        self.textures = []
        self.depth_texture = None
        
    def bind(self):
        """Bind framebuffer for rendering."""
        
    def unbind(self):
        """Unbind, return to default framebuffer."""
        
    def resize(self, width, height):
        """Resize framebuffer textures."""
        
    def release(self):
        """Release OpenGL resources."""
```

### 2. Brightness Extraction Shader

```glsl
// bright_extract.frag
#version 330

in vec2 v_tex;
out vec4 fragColor;

uniform sampler2D u_scene;
uniform float u_threshold;

void main() {
    vec4 color = texture(u_scene, v_tex);
    
    // Calculate brightness (luminance)
    float brightness = dot(color.rgb, vec3(0.2126, 0.7152, 0.0722));
    
    // Extract only bright pixels
    if (brightness > u_threshold) {
        fragColor = color;
    } else {
        fragColor = vec4(0.0);
    }
}
```

### 3. Gaussian Blur Shader (2-Pass)

```glsl
// blur.frag
#version 330

in vec2 v_tex;
out vec4 fragColor;

uniform sampler2D u_texture;
uniform vec2 u_direction;  // (1, 0) or (0, 1)
uniform vec2 u_texel_size;

// 9x9 Gaussian kernel weights
const float weights[5] = float[](
    0.227027,  // Center
    0.1945946, // 1 pixel
    0.1216216, // 2 pixels
    0.054054,  // 3 pixels
    0.016216   // 4 pixels
);

void main() {
    vec2 tex_offset = u_texel_size * u_direction;
    vec4 result = texture(u_texture, v_tex) * weights[0];
    
    for (int i = 1; i < 5; i++) {
        result += texture(u_texture, v_tex + tex_offset * i) * weights[i];
        result += texture(u_texture, v_tex - tex_offset * i) * weights[i];
    }
    
    fragColor = result;
}
```

### 4. Bloom Composite Shader

```glsl
// bloom.frag
#version 330

in vec2 v_tex;
out vec4 fragColor;

uniform sampler2D u_scene;      // Original scene
uniform sampler2D u_bloom;      // Blurred bright pixels
uniform float u_intensity;      // Bloom strength
uniform bool u_enabled;         // Bloom on/off

void main() {
    vec4 scene_color = texture(u_scene, v_tex);
    
    if (u_enabled) {
        vec4 bloom_color = texture(u_bloom, v_tex);
        scene_color += bloom_color * u_intensity;
    }
    
    // Tone mapping (optional, prevent clipping)
    scene_color.rgb = scene_color.rgb / (scene_color.rgb + vec3(1.0));
    
    fragColor = scene_color;
}
```

### 5. Post-Process Manager

```python
# src/render/post_process.py
class PostProcessManager:
    """
    Post-processing effects pipeline.
    
    Effects:
    - Bloom/Glow
    - Tone mapping
    - Color grading (future)
    - Vignette (future)
    """
    
    def __init__(self, ctx, width, height):
        # Framebuffers
        self.scene_fbo = Framebuffer(ctx, width, height, attachments=2)
        self.ping_fbo = Framebuffer(ctx, width//2, height//2)
        self.pong_fbo = Framebuffer(ctx, width//2, height//2)
        
        # Settings
        self.bloom_enabled = True
        self.bloom_intensity = 0.8
        self.bloom_threshold = 0.7
        self.blur_passes = 2
        
    def begin_frame(self):
        """Start capturing scene to FBO."""
        
    def end_frame(self):
        """Apply post-processing and render to screen."""
        
    def apply_bloom(self):
        """Bloom pipeline: extract -> blur -> composite."""
        
    def resize(self, width, height):
        """Resize all framebuffers."""
```

---

## 📊 Alt Görevler

| ID | Görev | Tahmini Süre | Sorumlu | Durum |
|----|-------|--------------|---------|-------|
| GFX-002-1 | Framebuffer class | 1 saat | Kemal | ⬜ |
| GFX-002-2 | Post vertex shader | 15 dk | Kemal | ⬜ |
| GFX-002-3 | Bright extract shader | 30 dk | Kemal | ⬜ |
| GFX-002-4 | Gaussian blur shader | 45 dk | Kemal | ⬜ |
| GFX-002-5 | Bloom composite shader | 30 dk | Kemal | ⬜ |
| GFX-002-6 | Post-process manager | 1.5 saat | Deniz | ⬜ |
| GFX-002-7 | Settings/config | 30 dk | Selin | ⬜ |
| GFX-002-8 | Demo entegrasyonu | 1 saat | Ekip | ⬜ |
| GFX-002-9 | Test & debug | 1 saat | Ekip | ⬜ |

**Toplam:** ~7 saat

---

## 🧪 Test Senaryoları

| Test | Açıklama | Beklenen Sonuç |
|------|----------|----------------|
| T1 | FBO oluşturma | Texture'lar doğru boyutta |
| T2 | Bright extract | Sadece parlak pikseller |
| T3 | Gaussian blur | Yumuşak blur efekti |
| T4 | Composite | Glow görünür |
| T5 | Performance | 60 FPS stable |
| T6 | Toggle on/off | Anında geçiş |
| T7 | Intensity control | 0-2 arası ayarlanabilir |

---

## 📐 API Tasarımı

```python
# Kullanım örneği
from src.render.post_process import PostProcessManager

# Post-process manager oluştur
post_fx = PostProcessManager(ctx, width, height)

# Ayarla
post_fx.bloom_enabled = True
post_fx.bloom_intensity = 0.8
post_fx.bloom_threshold = 0.7

# Render döngüsünde
post_fx.begin_frame()

# ... scene rendering here ...

post_fx.end_frame()
```

---

## ⚠️ Riskler ve Çözümler

| Risk | Etki | Çözüm |
|------|------|-------|
| Performans kaybı | Yüksek | Half-resolution blur |
| FBO bellek kullanımı | Orta | Tekstür boyutu optimizasyonu |
| Blur kalitesi | Düşük | Kernel size ayarlanabilir |

---

## 🔗 Bağımlılıklar

- [x] ModernGL context
- [x] Framebuffer support (GL 3.3)
- [x] GFX-001 Normal Mapping (tamamlandı)
- [ ] PIL (mevcut)

---

## ✅ Onay Bekleyen Noktalar

1. Blur çözünürlüğü: Full veya half? → **Half seçildi (performans)**
2. Blur pass sayısı: 1 veya 2? → **2 pass (kalite)**
3. Kernel boyutu: 5x5 veya 9x9? → **9x9 (daha yumuşak)**

---

## 📝 Notlar

- Celeste benzeri yumuşak glow efekti hedefleniyor
- Düşük performanslı cihazlarda otomatik kapanabilir
- Normal mapping ile birlikte kullanılabilir

---

**Plan Durumu:** ✅ ONAYLANDI (14 Mart 2025 - 18:20)

*Bu plan Kemal tarafından hazırlanmıştır.*
