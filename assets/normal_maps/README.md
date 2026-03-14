# Normal Maps Directory

Bu klasör sprite'lar için oluşturulan normal map'leri içerir.

## Kullanım

```bash
# Tek sprite için normal map oluştur
python -m src.render.normal_mapper assets/textures/player.png -o assets/normal_maps/player_normal.png

# Klasör için toplu işlem
python -m src.render.normal_mapper assets/textures/sprites/ -o assets/normal_maps/
```

## Normal Map Formatı

- **Format:** PNG RGB
- **R (Red):** X yönü (-1 → +1)
- **G (Green):** Y yönü (-1 → +1)  
- **B (Blue):** Z yönü (derinlik, genelde 1.0)

## Normal Map Oluşturma Seçenekleri

| Seçenek | Açıklama | Varsayılan |
|---------|----------|------------|
| `-s, --strength` | Normal şiddeti | 1.0 |
| `-b, --blur` | Blur yarıçapı | 0 |
| `--no-edge` | Edge detection kapat | Açık |
| `--invert` | Height map tersine çevir | Kapalı |

## Dosya Adlandırma

```
sprite.png        → sprite_normal.png
character.png     → character_normal.png
tile_floor.png    → tile_floor_normal.png
```

## İpuçları

1. **Daha keskin kenarlar:** `strength` değerini artırın (1.5 - 2.0)
2. **Daha yumuşak görünüm:** `--blur 2` kullanın
3. **Ters depth efekti:** `--invert` kullanın
