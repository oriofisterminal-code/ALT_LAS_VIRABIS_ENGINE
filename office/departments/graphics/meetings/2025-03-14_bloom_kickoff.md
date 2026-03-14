# 🎨 Grafik Departmanı - Toplantı #2

**Tarih:** 14 Mart 2025
**Saat:** 18:15
**Tür:** GFX-002 Post-Processing Kickoff
**Katılımcılar:** Dr. Elif (Moderatör), Kemal, Deniz, Selin

---

## 📋 Gündem

1. GFX-001 özeti ve başarıları
2. GFX-002 Post-Processing tanımı
3. Teknik yaklaşım ve algoritma
4. Kaynak ihtiyaçları
5. Görev dağılımı
6. Zaman çizelgesi

---

## 💬 Toplantı Notları

### Dr. Elif (Moderatör):
*"Arkadaşlar, GFX-001 Normal Mapping'i başarıyla tamamladık. Şimdi sıradaki görevimiz: Post-Processing ve Bloom efekti. Celeste tarzı bir glow sistemi oluşturacağız."*

### Kemal (GPU/Shader Uzmanı):
*"Bloom efekti için klasik yaklaşımı kullanacağız:*

*1. Render scene to framebuffer (FBO)
2. Extract bright pixels (brightness threshold)
3. Gaussian blur (2-pass: horizontal + vertical)
4. Composite: original + blurred bright pixels*

*GPU tarafında 3 shader gerekli: brightness extraction, blur, composite."*

### Deniz (Grafik Tasarımcı):
*"Blur kalitesi önemli. Gaussian blur için 9x9 kernel kullanalım. Daha yumuşak sonuç verir. Ayrıca bloom intensity ve threshold değerleri ayarlanabilir olmalı."*

### Selin (UX Uzmanı):
*"Kullanıcı için basit kontroller:*
*- Bloom Intensity slider (0.0 - 2.0)*
*- Brightness Threshold slider (0.0 - 1.0)*
*- Bloom On/Off toggle*

*Options menüsüne ekleyelim. Düşük performanslı cihazlarda kapalı başlasın."*

---

## 🎯 Kararlar

| Karar | Sorumlu | Tarih |
|-------|---------|-------|
| Framebuffer Object (FBO) sistemi kurulacak | Kemal | 14.03.2025 |
| Gaussian blur 9x9 kernel kullanılacak | Kemal | 14.03.2025 |
| Bloom intensity ve threshold ayarlanabilir | Selin | 14.03.2025 |
| Varsayılan: Bloom açık, intensity=0.8, threshold=0.7 | Ekip | 14.03.2025 |

---

## 📊 Teknik Tasarım

### Bloom Pipeline

```
┌─────────────────┐
│   Scene Render  │
│    (to FBO)     │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│   Bright Extract│  threshold > 0.7
│   Shader        │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Gaussian Blur   │  2-pass: H + V
│ 9x9 kernel      │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│   Composite     │  original + bloom
│   Shader        │  (additive blending)
└─────────────────┘
```

### Framebuffer Yapısı

```
FBO Setup:
- Color Attachment 0: Scene texture (RGBA16F)
- Color Attachment 1: Bright texture (RGBA16F)
- Ping-pong buffers for blur

Texture Sizes:
- Full resolution: 960x544
- Half resolution (blur): 480x272 (optimization)
```

---

## 📅 Zaman Çizelgesi

| Aşama | Tahmini Süre | Başlangıç | Bitiş |
|-------|--------------|-----------|-------|
| Framebuffer sistemi | 1 saat | Şimdi | - |
| Bright extract shader | 30 dk | - | - |
| Gaussian blur shader | 1 saat | - | - |
| Composite shader | 30 dk | - | - |
| Post-process manager | 1 saat | - | - |
| Demo entegrasyonu | 1 saat | - | - |
| Test ve debug | 1 saat | - | - |

**Toplam Tahmini:** 6 saat

---

## ✅ Aksiyon Maddeleri

| # | Aksiyon | Sorumlu | Deadline |
|---|---------|---------|----------|
| 1 | GFX-002 plan dosyası oluştur | Kemal | Şimdi |
| 2 | Framebuffer class yaz | Kemal | Bugün |
| 3 | Shader'ları yaz | Kemal | Bugün |
| 4 | Post-process manager | Deniz | Bugün |
| 5 | Demo entegrasyonu | Selin | Bugün |
| 6 | Test et | Ekip | Bugün |

---

## 📝 Notlar

- GFX-001 Normal Mapping ile uyumlu çalışmalı
- Performance hedefi: 60 FPS korunsun
- Düşük kalite modunda blur pass sayısı azaltılabilir

---

## 🔗 İlgili Dosyalar

- `office/departments/graphics/plans/GFX-002_plan.md` (oluşturulacak)
- `docs/ROADMAP.md`
- `src/render/normal_renderer.py` (GFX-001)

---

*Toplantı notları Dr. Elif tarafından tutulmuştur.*
