# 🎨 Grafik Departmanı - Toplantı #1

**Tarih:** 14 Mart 2025
**Saat:** 15:45
**Tür:** Kickoff Toplantısı
**Katılımcılar:** Dr. Elif (Moderatör), Kemal, Deniz, Selin

---

## 📋 Gündem

1. Grafik departmanı hedefleri
2. Görev öncelikleri
3. Referans oyunlar ve teknikler
4. Kaynak ihtiyaçları
5. Zaman çizelgesi

---

## 💬 Toplantı Notları

### Dr. Elif (Moderatör):
*"Arkadaşlar, grafik departmanı için üç önemli özellik ekleyeceğiz: Normal Maps, Post-Processing ve Dynamic Shadows. Bunlar profesyonel seviye grafik kalitesi için gerekli."*

### Kemal (GPU/Shader Uzmanı):
*"Normal mapping 2D için tricky ama yapılabilir. Octopath Traveler'ın tekniğini inceledim - sprite'lara depth vermek için height map'ten normal map üretebiliriz. GPU tarafında ekstra bir pass gerektirecek."*

*"Shadow sistemi için shadow mapping kullanacağız. 2D için basitleştirilmiş bir versiyon, çok pahalı değil."*

### Deniz (Grafik Tasarımcı):
*"Post-processing için bloom efektini framebuffer ile yapacağız. Celeste'in yaklaşımı: bright pixel'leri çıkar, blur'la, orijinal ile birleştir. Çok güzel bir glow efekti veriyor."*

*"Normal map'ler için bir tool yazmalıyım. Texture'dan otomatik normal map üreten bir şey. Artist'ler elle çizmesin."*

### Selin (UX Uzmanı):
*"Kullanıcı deneyimi için bu efektlerin ayarlanabilir olması önemli. Options menüsünde 'Bloom Intensity', 'Shadow Quality' gibi slider'lar olmalı."*

*"Ayrıca düşük performanslı cihazlar için bu efektleri kapatma seçeneği ekleyelim."*

---

## 🎯 Kararlar

| Karar | Sorumlu | Tarih |
|-------|---------|-------|
| Normal mapping için height-to-normal generator yazılacak | Deniz | - |
| Shadow sistemi için PCF soft shadows kullanılacak | Kemal | - |
| Post-processing için FBO (Framebuffer Object) kullanılacak | Kemal | - |
| Tüm efektler ayarlanabilir olacak | Selin | - |
| Performans hedefi: 60 FPS sabit | Ekip | - |

---

## 📊 Teknik Kararlar

### Normal Mapping (Octopath Traveler tarzı)
```
Pipeline:
1. Texture → Height Map (grayscale)
2. Height Map → Normal Map (Sobel filter)
3. Render with normal map + dynamic light
4. Result: 3D-like lighting on 2D sprite
```

### Post-Processing (Celeste tarzı)
```
Pipeline:
1. Render scene to FBO
2. Extract bright pixels (threshold)
3. Gaussian blur (2-pass)
4. Composite: original + blurred bright
```

### Dynamic Shadows (Hollow Knight tarzı)
```
Pipeline:
1. Light position → Shadow map (depth)
2. Render scene with shadow comparison
3. PCF filtering for soft edges
4. Cascade for large maps (optional)
```

---

## 📅 Zaman Çizelgesi

| Görev | Tahmini Süre | Başlangıç | Bitiş |
|-------|--------------|-----------|-------|
| GFX-001 Normal Maps | 3 gün | - | - |
| GFX-002 Post-Processing | 2 gün | - | - |
| GFX-003 Dynamic Shadows | 4 gün | - | - |

**Toplam Tahmini:** 9 gün

---

## ✅ Aksiyon Maddeleri

| # | Aksiyon | Sorumlu | Deadline |
|---|---------|---------|----------|
| 1 | GFX-001 plan dosyası oluştur | Kemal | Sonraki toplantı |
| 2 | Normal map generator araştır | Deniz | Sonraki toplantı |
| 3 | Bloom shader örnekleri bul | Kemal | Sonraki toplantı |
| 4 | Performance metrics belirle | Selin | Sonraki toplantı |

---

## 📝 Sonraki Toplantı

**Tarih:** Belirlenecek
**Gündem:** GFX-001 Normal Mapping Plan Onayı
**Hazırlıklar:** Plan dosyası hazır olmalı

---

## 🔗 İlgili Dosyalar

- `office/departments/graphics/TASKS.md`
- `office/departments/graphics/plans/GFX-001_plan.md` (oluşturulacak)
- `docs/ROADMAP.md`

---

*Toplantı notları Dr. Elif tarafından tutulmuştur.*
