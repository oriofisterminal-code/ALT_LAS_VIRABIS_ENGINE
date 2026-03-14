# 🎨 Grafik Departmanı - Durum

> Son güncelleme: 14 Mart 2025
> Departman Sorumlusu: Kemal

---

## 📊 Departman Özeti

Grafik departmanı, motorun görsel özelliklerini geliştirir.
Referans oyunlar: Hyper Light Drifter, Celeste, Octopath Traveler, Hollow Knight

---

## 👥 Ekip

| Uzman | Yardımcı Meslek | Karakter | Sorumluluk |
|-------|-----------------|----------|------------|
| **Kemal** | İnşaat Mühendisi | Sabırsız, işini sever | GPU, Shader'lar |
| **Deniz** | Grafik Tasarımcı | Yaratıcı | Renkler, UI, ASCII art |
| **Selin** | Psikolog | Mükemmeliyetçi | UX, Tips, Animasyonlar |

---

## 📈 Grafik Özellikleri Durumu

| Özellik | Referans | Durum | Atanan |
|---------|----------|-------|--------|
| Base Rendering | Undertale | ✅ Tamamlandı | - |
| Dynamic Lighting | Hyper Light Drifter | 🔄 Kısmen | Kemal |
| **Normal Maps** | Octopath Traveler | 📋 Planlanıyor | - |
| **Post-Processing** | Celeste | 📋 Planlanıyor | - |
| Particle System | Stardew Valley | 🔄 Kısmen | Deniz |
| **Dynamic Shadows** | Hollow Knight | 📋 Planlanıyor | - |

---

## 🔄 Aktif Görevler

### GFX-001: Normal Mapping Sistemi ❌ BEKLİYOR
- Öncelik: Yüksek
- Referans: Octopath Traveler
- Durum: Planlanıyor

### GFX-002: Post-Processing (Bloom) ❌ BEKLİYOR
- Öncelik: Yüksek
- Referans: Celeste
- Durum: Planlanıyor

### GFX-003: Dynamic Shadows ❌ BEKLİYOR
- Öncelik: Orta
- Referans: Hollow Knight
- Durum: Planlanıyor

---

## ✅ Tamamlanan Görevler

| ID | Görev | Tamamlanma | Sorumlu |
|----|-------|------------|---------|
| GFX-000 | Base Rendering | v0.2.0 | - |
| - | Point Lights | v0.2.0 | Kemal |
| - | Basic Particles | v0.2.0 | Deniz |
| - | Loading Screen | v0.3.1 | Kemal, Selin |

---

## 📂 Departman Dosya Yapısı

```
office/departments/graphics/
├── STATUS.md          # Bu dosya
├── TASKS.md           # Görev detayları
├── plans/             # Plan dosyaları
│   ├── GFX-001_plan.md
│   ├── GFX-002_plan.md
│   └── GFX-003_plan.md
├── meetings/          # Toplantı notları
│   └── YYYY-MM-DD_topic.md
└── tests/             # Test sonuçları
```

---

## 🔗 Teknik Kaynaklar

### Shader Dosyaları
- `src/render/base.frag` - Temel fragment shader
- `src/render/base.vert` - Temel vertex shader
- `src/render/glow.frag` - Glow effect (mevcut)
- `src/render/light.frag` - Lighting shader
- `src/render/water.frag` - Water effect

### Render Kodları
- `src/render/adapter.py` - GPU render adapter
- `src/render/effects.py` - Effect sistemi
- `src/render/lights.py` - Işık sistemi
- `src/render/particles.py` - Parçacık sistemi

---

## 📊 Performans Hedefleri

| Metrik | Mevcut | Hedef |
|--------|--------|-------|
| FPS (Demo) | 60 | 60 |
| Draw Calls | ~200 | <300 |
| Memory | ~50MB | <100MB |
| Startup | ~2s | <1.5s |

---

## 📝 Notlar

- GPU shader'ları GLSL 3.30 kullanıyor
- ModernGL 5.8+ gerekli
- SDL2 veya GLFW backend kullanılabilir

---

*Bu dosya her departman toplantısında güncellenmelidir.*
