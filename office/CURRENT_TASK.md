# 📋 ALT_LAS ENGINE - Mevcut Görev

> Son güncelleme: 14 Mart 2025
> Departman: Graphics
> Görev ID: GFX-002

---

## 🎯 Aktif Görev

### GFX-002: Post-Processing (Bloom/Glow)

| Alan | Bilgi |
|------|-------|
| **Departman** | Graphics |
| **Referans Oyun** | Celeste |
| **Öncelik** | Yüksek |
| **Durum** | 📋 PLANLANIYOR |
| **Başlangıç** | - |
| **Bitiş Tahmini** | - |

---

## 📝 Görev Açıklaması

Framebuffer tabanlı bloom/glow post-processing efekti oluşturulacak. Bu sistem:
- Bright pixel'leri ayıklayıp blur uygulayacak
- Orijinal görüntü ile birleştirecek
- Ayarlanabilir intensity ve threshold sağlayacak

---

## 🔄 İş Akışı Durumu

```
[1] TOPLANTI     → ⬜ Bekliyor
[2] PLANLAMA     → ⬜ Bekliyor
[3] ONAY         → ⬜ Bekliyor
[4] UYGULAMA     → ⬜ Bekliyor
[5] TEST         → ⬜ Bekliyor
[6] TAMAMLANDI   → ⬜ Bekliyor
```

---

## 📋 Alt Görevler

| ID | Görev | Sorumlu | Durum |
|----|-------|---------|-------|
| GFX-002-1 | Framebuffer sistemi | Kemal | ⬜ |
| GFX-002-2 | Brightness extraction shader | Kemal | ⬜ |
| GFX-002-3 | Gaussian blur shader | Kemal | ⬜ |
| GFX-002-4 | Bloom composite shader | Kemal | ⬜ |
| GFX-002-5 | Post-process manager | Deniz | ⬜ |
| GFX-002-6 | Demo entegrasyonu | Selin | ⬜ |

---

## 📂 İlgili Dosyalar

```
src/render/
├── framebuffer.py      # FBO yönetimi
├── bloom.frag         # Bloom shader
├── blur.frag          # Gaussian blur
├── bright_extract.frag # Brightness extraction
└── post_process.py    # Post-process pipeline
```

---

## 🔗 Bağımlılıklar

- [x] ModernGL context
- [x] Framebuffer support (GL 3.3)
- [x] GFX-001 Normal Mapping (tamamlandı)

---

## 📊 İlerleme

```
Toplam İlerleme: 0%
████████░░░░░░░░░░░░░░░░░░░░░░░░░░ 0%
```

---

## ✅ Önceki Görev Tamamlandı

**GFX-001: Normal Mapping** ✅

| Dosya | Durum |
|-------|-------|
| `src/render/normal_map.vert` | ✅ Oluşturuldu |
| `src/render/normal_map.frag` | ✅ Oluşturuldu |
| `src/render/normal_mapper.py` | ✅ Oluşturuldu |
| `src/render/normal_renderer.py` | ✅ Oluşturuldu |
| `assets/normal_maps/README.md` | ✅ Oluşturuldu |

---

## 📝 Notlar

Henüz başlanmadı. İlk toplantı yapılacak.

---

## 🚀 Sonraki Adım

**Toplantı çağrısı yapılacak!**
- Tarih: Belirlenecek
- Katılımcılar: Grafik ekibi
- Gündem: Bloom post-processing planlaması

---

*Bu dosya görev tamamlandığında arşivlenip yenisine geçilecektir.*
