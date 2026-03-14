"""
ALT_LAS Engine - Sprite Generator
Creates sample placeholder sprites for testing.
"""

import os
from PIL import Image, ImageDraw

BASE_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TEXTURES_DIR = os.path.join(BASE_DIR, "Content", "Textures")


def create_tile(name: str, color: tuple, pattern: str = "solid") -> Image.Image:
    """Create a 16x16 tile sprite."""
    img = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    if pattern == "solid":
        draw.rectangle([0, 0, 15, 15], fill=color)
    elif pattern == "brick":
        draw.rectangle([0, 0, 15, 15], fill=color)
        draw.line([(0, 4), (15, 4)], fill=(0, 0, 0, 100), width=1)
        draw.line([(0, 10), (15, 10)], fill=(0, 0, 0, 100), width=1)
        draw.line([(8, 0), (8, 4)], fill=(0, 0, 0, 100), width=1)
        draw.line([(4, 4), (4, 10)], fill=(0, 0, 0, 100), width=1)
        draw.line([(12, 4), (12, 10)], fill=(0, 0, 0, 100), width=1)
    elif pattern == "stone":
        draw.rectangle([0, 0, 15, 15], fill=color)
        for i in range(4):
            for j in range(4):
                if (i + j) % 2 == 0:
                    shade = tuple(max(0, c - 20) for c in color[:3]) + (color[3],)
                    draw.rectangle([i*4, j*4, i*4+3, j*4+3], fill=shade)
    elif pattern == "water":
        draw.rectangle([0, 0, 15, 15], fill=color)
        for y in range(0, 16, 3):
            offset = (y // 3) % 2 * 4
            for x in range(offset, 16, 8):
                draw.arc([x, y, x+6, y+4], 0, 180, fill=(255, 255, 255, 80), width=1)
    elif pattern == "trigger":
        draw.rectangle([0, 0, 15, 15], fill=color)
        draw.rectangle([4, 4, 11, 11], outline=(255, 255, 255, 150), width=1)

    return img


def create_npc(name: str, color: tuple, style: str = "simple") -> Image.Image:
    """Create a 16x16 NPC sprite."""
    img = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    if style == "flower":
        # Flower-like NPC (like Flowey)
        draw.ellipse([4, 6, 12, 14], fill=color)  # Face
        draw.ellipse([2, 2, 6, 8], fill=(255, 255, 0, 200))  # Petal 1
        draw.ellipse([10, 2, 14, 8], fill=(255, 255, 0, 200))  # Petal 2
        draw.ellipse([6, 0, 10, 4], fill=(255, 255, 0, 200))  # Petal 3
        draw.ellipse([6, 8, 7, 10], fill=(0, 0, 0, 255))  # Eye 1
        draw.ellipse([9, 8, 10, 10], fill=(0, 0, 0, 255))  # Eye 2
        draw.arc([5, 10, 11, 13], 0, 180, fill=(0, 0, 0, 255), width=1)  # Smile
    elif style == "skeleton":
        # Skeleton NPC
        draw.ellipse([3, 1, 13, 11], fill=(255, 255, 255, 255))  # Skull
        draw.ellipse([4, 4, 6, 7], fill=(0, 0, 0, 255))  # Eye socket 1
        draw.ellipse([10, 4, 12, 7], fill=(0, 0, 0, 255))  # Eye socket 2
        draw.ellipse([7, 7, 9, 9], fill=(0, 0, 0, 255))  # Nose
        draw.rectangle([5, 10, 11, 12], fill=(0, 0, 0, 200))  # Mouth
        draw.rectangle([3, 12, 13, 16], fill=(255, 255, 255, 255))  # Body
    elif style == "guard":
        # Guard NPC
        draw.rectangle([4, 2, 12, 14], fill=color)  # Body
        draw.ellipse([5, 0, 11, 6], fill=(255, 200, 150, 255))  # Face
        draw.rectangle([4, 0, 12, 2], fill=(100, 100, 100, 255))  # Helmet
        draw.rectangle([3, 6, 5, 12], fill=(150, 150, 150, 255))  # Shield
    else:
        # Simple NPC
        draw.ellipse([3, 0, 13, 10], fill=color)  # Head
        draw.rectangle([5, 10, 11, 16], fill=color)  # Body
        draw.ellipse([5, 3, 7, 6], fill=(0, 0, 0, 255))  # Eye 1
        draw.ellipse([9, 3, 11, 6], fill=(0, 0, 0, 255))  # Eye 2

    return img


def create_player_heart() -> Image.Image:
    """Create player heart sprite for bullet hell."""
    img = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # Heart shape
    draw.ellipse([2, 2, 8, 8], fill=(255, 0, 0, 255))  # Left bump
    draw.ellipse([8, 2, 14, 8], fill=(255, 0, 0, 255))  # Right bump
    draw.polygon([(2, 6), (14, 6), (8, 14)], fill=(255, 0, 0, 255))  # Bottom

    # Shine
    draw.ellipse([4, 3, 6, 5], fill=(255, 150, 150, 200))  # Shine

    return img


def create_bullet(style: str = "circle") -> Image.Image:
    """Create bullet sprite for bullet hell."""
    img = Image.new("RGBA", (8, 8), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    if style == "circle":
        draw.ellipse([0, 0, 7, 7], fill=(255, 255, 255, 255))
    elif style == "bone":
        draw.rectangle([2, 0, 6, 8], fill=(255, 255, 255, 255))
        draw.ellipse([1, 0, 7, 3], fill=(255, 255, 255, 255))
        draw.ellipse([1, 5, 7, 8], fill=(255, 255, 255, 255))
    elif style == "heart":
        draw.ellipse([0, 1, 3, 4], fill=(255, 100, 100, 255))
        draw.ellipse([4, 1, 7, 4], fill=(255, 100, 100, 255))
        draw.polygon([(1, 3), (6, 3), (3, 7)], fill=(255, 100, 100, 255))

    return img


def create_ui_element(element: str, width: int = 100, height: int = 20) -> Image.Image:
    """Create UI element sprite."""
    img = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    if element == "hp_bar_bg":
        draw.rectangle([0, 0, width-1, height-1], outline=(255, 255, 255, 200), width=2)
        draw.rectangle([2, 2, width-3, height-3], fill=(50, 0, 0, 200))

    elif element == "hp_bar_fill":
        draw.rectangle([0, 0, width-1, height-1], fill=(255, 0, 0, 255))

    elif element == "battle_box":
        draw.rectangle([0, 0, width-1, height-1], outline=(255, 255, 255, 255), width=3)
        draw.rectangle([3, 3, width-4, height-4], fill=(0, 0, 0, 255))

    elif element == "button":
        draw.rectangle([0, 0, width-1, height-1], fill=(100, 100, 100, 200), outline=(255, 255, 255, 100), width=1)

    return img


def main():
    """Generate all placeholder sprites."""
    print("Generating placeholder sprites...")

    # Create directories
    for subdir in ["Tiles", "NPCs", "Battle", "UI"]:
        os.makedirs(os.path.join(TEXTURES_DIR, subdir), exist_ok=True)

    # Tiles
    tiles = [
        ("floor_stone.png", (80, 80, 80, 255), "stone"),
        ("floor_wood.png", (139, 90, 43, 255), "solid"),
        ("wall_brick.png", (100, 100, 100, 255), "brick"),
        ("wall_stone.png", (90, 90, 90, 255), "stone"),
        ("water.png", (50, 100, 200, 255), "water"),
        ("trigger.png", (200, 200, 100, 100), "trigger"),
        ("door.png", (139, 69, 19, 255), "solid"),
    ]

    for name, color, pattern in tiles:
        img = create_tile(name, color, pattern)
        path = os.path.join(TEXTURES_DIR, "Tiles", name)
        img.save(path)
        print(f"  Created: Tiles/{name}")

    # NPCs
    npcs = [
        ("flowey.png", (255, 255, 0, 255), "flower"),
        ("skeleton.png", (255, 255, 255, 255), "skeleton"),
        ("guard.png", (0, 150, 0, 255), "guard"),
        ("guide.png", (0, 200, 200, 255), "simple"),
        ("npc_generic.png", (0, 255, 0, 255), "simple"),
    ]

    for name, color, style in npcs:
        img = create_npc(name, color, style)
        path = os.path.join(TEXTURES_DIR, "NPCs", name)
        img.save(path)
        print(f"  Created: NPCs/{name}")

    # Battle sprites
    player_heart = create_player_heart()
    player_heart.save(os.path.join(TEXTURES_DIR, "Battle", "player_heart.png"))
    print("  Created: Battle/player_heart.png")

    # Player sprite (larger)
    player_img = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    draw = ImageDraw.Draw(player_img)
    draw.ellipse([8, 2, 24, 18], fill=(255, 200, 100, 255))  # Head
    draw.rectangle([10, 18, 22, 30], fill=(100, 100, 255, 255))  # Body
    draw.rectangle([8, 18, 12, 28], fill=(100, 100, 255, 255))  # Arm L
    draw.rectangle([20, 18, 24, 28], fill=(100, 100, 255, 255))  # Arm R
    draw.ellipse([12, 6, 14, 10], fill=(0, 0, 0, 255))  # Eye L
    draw.ellipse([18, 6, 20, 10], fill=(0, 0, 0, 255))  # Eye R
    draw.arc([12, 11, 20, 16], 0, 180, fill=(0, 0, 0, 255), width=1)  # Smile
    player_img.save(os.path.join(TEXTURES_DIR, "Battle", "player.png"))
    print("  Created: Battle/player.png")

    # Bullets
    bullets = [
        ("bullet_circle.png", "circle"),
        ("bullet_bone.png", "bone"),
        ("bullet_heart.png", "heart"),
    ]
    for name, style in bullets:
        img = create_bullet(style)
        img.save(os.path.join(TEXTURES_DIR, "Battle", name))
        print(f"  Created: Battle/{name}")

    # UI elements
    ui_elements = [
        ("hp_bar_bg.png", "hp_bar_bg", 100, 20),
        ("hp_bar_fill.png", "hp_bar_fill", 100, 20),
        ("battle_box.png", "battle_box", 200, 100),
        ("button.png", "button", 80, 24),
    ]
    for name, element, w, h in ui_elements:
        img = create_ui_element(element, w, h)
        img.save(os.path.join(TEXTURES_DIR, "UI", name))
        print(f"  Created: UI/{name}")

    print(f"\nAll sprites generated in: {TEXTURES_DIR}")
    print(f"Total: {len(tiles) + len(npcs) + 5 + len(bullets) + len(ui_elements)} sprites")


if __name__ == "__main__":
    main()
