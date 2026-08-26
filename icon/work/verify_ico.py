from pathlib import Path

from PIL import Image, ImageDraw


ICON = Path(__file__).parents[1] / "outputs" / "QPeek.ico"
PREVIEW = Path(__file__).with_name("ico-preview.png")
SIZES = [16, 24, 32, 48, 64, 128, 256]

tile = 288
label_height = 28
sheet = Image.new("RGB", (tile * 4, (tile + label_height) * 2), "#d8d8d8")
draw = ImageDraw.Draw(sheet)

with Image.open(ICON) as icon:
    for index, size in enumerate(SIZES):
        frame = icon.ico.getimage((size, size)).convert("RGBA")
        if frame.size != (size, size):
            raise RuntimeError(f"Wrong frame size for {size}px: {frame.size}")

        x = (index % 4) * tile
        y = (index // 4) * (tile + label_height)
        cell = Image.new("RGB", (tile, tile), "white")
        scaled = frame.resize((size * min(8, 256 // size),) * 2, Image.Resampling.NEAREST)
        px = (tile - scaled.width) // 2
        py = (tile - scaled.height) // 2
        cell.paste(scaled, (px, py), scaled)
        sheet.paste(cell, (x, y))
        draw.text((x + 8, y + tile + 5), f"{size} x {size}", fill="black")

sheet.save(PREVIEW)
print(f"Preview: {PREVIEW}")
