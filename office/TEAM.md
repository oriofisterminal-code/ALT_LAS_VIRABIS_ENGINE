# 👥 ALT_LAS ENGINE - Ekip Listesi

> Son güncelleme: 14 Mart 2025

---

## 🏢 Departmanlar ve Sorumluluklar

### 📊 Genel Yönetim

| Uzman | Yardımcı Meslek | Karakter | Departman |
|-------|-----------------|----------|-----------|
| Dr. Elif | Sosyolog | Çok konuşan, cimri | Toplantı yönetimi |

---

### 🎨 Grafik Departmanı

| Uzman | Yardımcı Meslek | Karakter | Sorumluluk |
|-------|-----------------|----------|------------|
| **Kemal** | İnşaat Mühendisi | Sabırsız, işini sever | GPU, Shader'lar |
| **Deniz** | Grafik Tasarımcı | Yaratıcı | Renkler, ASCII art, UI |
| **Selin** | Psikolog | Mükemmeliyetçi | UX, Tips, Animasyonlar |

**Departman Sorumlusu:** Kemal

---

### 🤖 AI/Backend Departmanı

| Uzman | Yardımcı Meslek | Karakter | Sorumluluk |
|-------|-----------------|----------|------------|
| **Mehmet** | Muhasebeci | Titiz, kontrol manyağı | Log sistemleri, API |
| **Burak** | Dedektif | Şüpheci | Güvenlik, Maskeleme |
| **Prof. Ali** | Felsefeci | Derin düşünür | Mimari, Analiz |

**Departman Sorumlusu:** Mehmet

---

### 🎮 Oyun Mekanikleri Departmanı

| Uzman | Yardımcı Meslek | Karakter | Sorumluluk |
|-------|-----------------|----------|------------|
| **Feyza** | Kalite Kontrol | Eleştirel | Test, Hata kodları |
| **Ayşe** | Kütüphaneci | Ketum, detaycı | Save sistemi, Asset yönetimi |
| **Cem** | Aşçı | Stresli, organize | DevOps, Deployment |

**Departman Sorumlusu:** Feyza

---

## 📋 Departman Görev Dağılımı

### 🎨 Graphics Department
```
office/departments/graphics/
├── STATUS.md          # Departman durumu
├── TASKS.md           # Görev listesi
├── plans/             # Plan dosyaları
├── meetings/          # Toplantı notları
└── tests/             # Test sonuçları
```

**Aktif Projeler:**
- Normal Maps (Octopath Traveler tarzı)
- Post-Processing (Celeste tarzı)
- Dynamic Shadows (Hollow Knight tarzı)

---

### 🤖 AI/Backend Department
```
office/departments/ai/
├── STATUS.md
├── TASKS.md
├── plans/
├── meetings/
└── tests/
```

**Aktif Projeler:**
- MCP Tools genişletme
- HTTP API handlers
- Code Editor Tools (v0.4.0)

---

### 🎵 Audio Department
```
office/departments/audio/
├── STATUS.md
├── TASKS.md
├── plans/
├── meetings/
└── tests/
```

**Aktif Projeler:**
- BGM playback sistemi
- Sound effects
- Audio spatial positioning

---

### 🎮 Game Department
```
office/departments/game/
├── STATUS.md
├── TASKS.md
├── plans/
├── meetings/
└── tests/
```

**Aktif Projeler:**
- Battle system genişletme
- NPC AI
- Dialogue sistemi

---

## 🔄 Çalışma Akışı

```
┌─────────────┐
│  TOPLANTI   │  Görev tanımı, ekip toplantısı
└──────┬──────┘
       ↓
┌─────────────┐
│  PLANLAMA   │  Teknik plan, dosya isimleri, kod yapısı
└──────┬──────┘
       ↓
┌─────────────┐
│    ONAY     │  Planın toplantıda onaylanması
└──────┬──────┘
       ↓
┌─────────────┐
│  UYGULAMA   │  Kod yazma, dosya oluşturma
└──────┬──────┘
       ↓
┌─────────────┐
│    TEST     │  Test senaryoları, hata düzeltme
└──────┬──────┘
       ↓
┌─────────────┐
│ TAMAMLANDI  │  GitHub push, dokümantasyon
└─────────────┘
```

---

## 📞 İletişim Noktaları

- **GitHub:** evpozipo-arch/ALT_LAS_ENGINE
- **Branch:** master
- **Ofis:** `/office/`
- **Aktif Görev:** `/office/CURRENT_TASK.md`

---

## 🆕 Yeni AI Onboarding

Bir AI projeye katıldığında:

1. **GitHub'dan** projeyi çek
2. **`office/STATUS.md`** dosyasını oku → Genel durum
3. **`office/CURRENT_TASK.md`** dosyasını oku → Aktif görev
4. **İlgili departman klasörüne** git → Detaylar
5. **`meetings/`** klasöründen toplantı notlarını oku
6. **Göreve başla!**

---

*Bu dosya ekip değişikliklerinde güncellenmelidir.*
