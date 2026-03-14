# Custom ASCII Art & Fonts

Bu klasör terminal tabanlı render için özel ASCII art sprite'ları ve font dosyalarını içerir.

## ASCII Art Sprites

Terminal modunda kullanılan ASCII karakter sprite'ları:

### Battle Sprites
- `soul_heart.txt` - Player soul (♥)
- `bullet_types.txt` - Farklı mermi tipleri
- `battle_box.txt` - Savaş alanı çerçevesi

### NPC Portraits
- ASCII portreler için `portraits/` klasörü

### UI Elements
- Menü çerçeveleri
- HP bar karakterleri
- Düğme stilleri

## Font Files

Terminal için optimize edilmiş font dosyaları:

- `monospace_16.txt` - 16x16 monospace
- `pixel_8.txt` - 8x8 pixel font
- `hud_font.txt` - HUD için özel font

## Kullanım

```python
from Content.Textures.Custom import ascii_loader

# ASCII sprite yükle
sprite = ascii_loader.load("soul_heart.txt")
print(sprite.render())

# Font yükle
font = ascii_loader.load_font("pixel_8")
text = font.render("HELLO", color="red")
```
