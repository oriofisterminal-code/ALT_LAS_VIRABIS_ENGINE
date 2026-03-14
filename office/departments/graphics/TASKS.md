# 🎨 Grafik Departmanı - Görev Listesi

> Son güncelleme: 14 Mart 2025

---

## 📋 Görev Sırası

Görevler bu sırayla işlenecektir:

| Sıra | ID | Görev | Öncelik | Durum |
|------|-----|-------|---------|-------|
| 1 | GFX-001 | Normal Mapping | 🔴 Yüksek | ✅ Tamamlandı |
| 2 | GFX-002 | Post-Processing (Bloom) | 🔴 Yüksek | 📋 Sırada |
| 3 | GFX-003 | Dynamic Shadows | 🟡 Orta | 📋 Planlanıyor |
| 4 | GFX-004 | Sprite Animation | 🟡 Orta | 📋 Planlanıyor |
| 5 | GFX-005 | Parallax Backgrounds | 🟢 Düşük | 📋 Planlanıyor |

---

## 📝 Görev Detayları

---

### GFX-001: Normal Mapping Sistemi

| Alan | Bilgi |
|------|-------|
| **ID** | GFX-001 |
| **Referans** | Octopath Traveler |
| **Öncelik** | 🔴 Yüksek |
| **Durum** | 📋 Planlanıyor |
| **Sorumlu** | - |
| **Başlangıç** | - |
| **Bitiş** | - |

**Açıklama:**
2D sprite'larda normal map kullanarak dinamik ışıklandırma efekti.

**Alt Görevler:**
- [ ] GFX-001-1: Normal map vertex shader
- [ ] GFX-001-2: Normal map fragment shader
- [ ] GFX-001-3: Normal map generator (texture'den)
- [ ] GFX-001-4: Sprite entegrasyonu
- [ ] GFX-001-5: Demo scene testi

**Çıktı Dosyaları:**
```
src/render/normal_map.vert
src/render/normal_map.frag
src/render/normal_mapper.py
```

---

### GFX-002: Post-Processing (Bloom)

| Alan | Bilgi |
|------|-------|
| **ID** | GFX-002 |
| **Referans** | Celeste |
| **Öncelik** | 🔴 Yüksek |
| **Durum** | 📋 Planlanıyor |
| **Sorumlu** | - |
| **Başlangıç** | - |
| **Bitiş** | - |

**Açıklama:**
Framebuffer tabanlı bloom/glow post-processing efekti.

**Alt Görevler:**
- [ ] GFX-002-1: Framebuffer oluşturma
- [ ] GFX-002-2: Brightness extraction shader
- [ ] GFX-002-3: Gaussian blur shader
- [ ] GFX-002-4: Bloom composite shader
- [ ] GFX-002-5: Post-process manager

**Çıktı Dosyaları:**
```
src/render/framebuffer.py
src/render/bloom.frag
src/render/blur.frag
src/render/post_process.py
```

---

### GFX-003: Dynamic Shadows

| Alan | Bilgi |
|------|-------|
| **ID** | GFX-003 |
| **Referans** | Hollow Knight |
| **Öncelik** | 🟡 Orta |
| **Durum** | 📋 Planlanıyor |
| **Sorumlu** | - |
| **Başlangıç** | - |
| **Bitiş** | - |

**Açıklama:**
Gerçek zamanlı dinamik gölge sistemi.

**Alt Görevler:**
- [ ] GFX-003-1: Shadow map framebuffer
- [ ] GFX-003-2: Shadow depth shader
- [ ] GFX-003-3: PCF soft shadows
- [ ] GFX-003-4: Shadow cascade (büyük haritalar)
- [ ] GFX-003-5: Shadow manager

**Çıktı Dosyaları:**
```
src/render/shadow_map.py
src/render/shadow_depth.frag
src/render/shadow_depth.vert
src/render/shadows.py
```

---

### GFX-004: Sprite Animation System

| Alan | Bilgi |
|------|-------|
| **ID** | GFX-004 |
| **Referans** | Stardew Valley |
| **Öncelik** | 🟡 Orta |
| **Durum** | 📋 Planlanıyor |
| **Sorumlu** | - |

**Açıklama:**
Sprite sheet tabanlı animasyon sistemi.

**Alt Görevler:**
- [ ] GFX-004-1: Sprite sheet loader
- [ ] GFX-004-2: Animation state machine
- [ ] GFX-004-3: Frame timing system
- [ ] GFX-004-4: Animation blending

---

### GFX-005: Parallax Backgrounds

| Alan | Bilgi |
|------|-------|
| **ID** | GFX-005 |
| **Referans** | Hollow Knight |
| **Öncelik** | 🟢 Düşük |
| **Durum** | 📋 Planlanıyor |
| **Sorumlu** | - |

**Açıklama:**
Katmanlı paralaks arka plan sistemi.

**Alt Görevler:**
- [ ] GFX-005-1: Layer system
- [ ] GFX-005-2: Parallax camera
- [ ] GFX-005-3: Background rendering
- [ ] GFX-005-4: Scrolling optimization

---

## 🔄 İş Akışı Şablonu

Her görev için bu akış takip edilecek:

```
1. TOPLANTI     → Görev tanımı, ekip ataması
2. PLANLAMA     → Teknik plan dosyası oluştur
3. ONAY         → Toplantıda planı onayla
4. UYGULAMA     → Kodu yaz
5. TEST         → Test et, hataları düzelt
6. PUSH         → GitHub'a push et
7. ARŞİV        → Görevi tamamlandı işaretle
```

---

## 📊 İlerleme Takibi

```
GFX-001: ░░░░░░░░░░░░░░░░░░░░ 0%
GFX-002: ░░░░░░░░░░░░░░░░░░░░ 0%
GFX-003: ░░░░░░░░░░░░░░░░░░░░ 0%
GFX-004: ░░░░░░░░░░░░░░░░░░░░ 0%
GFX-005: ░░░░░░░░░░░░░░░░░░░░ 0%

Toplam: 0/5 görev tamamlandı
```

---

*Bu dosya görev durumları değiştikçe güncellenmelidir.*
