from pathlib import Path

from PIL import Image


SOURCE = Path(__file__).with_name("image.png")
OUTPUT = Path(__file__).parents[1] / "outputs" / "QPeek.ico"
SIZES = [(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)]


with Image.open(SOURCE) as source:
    source.load()
    if source.width != source.height:
        side = max(source.size)
        canvas = Image.new("RGBA", (side, side), (0, 0, 0, 0))
        x = (side - source.width) // 2
        y = (side - source.height) // 2
        canvas.alpha_composite(source.convert("RGBA"), (x, y))
        icon_source = canvas
    else:
        icon_source = source.convert("RGBA")

    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    icon_source.save(OUTPUT, format="ICO", sizes=SIZES, bitmap_format="png")

with Image.open(OUTPUT) as icon:
    embedded = sorted(icon.ico.sizes())

expected = sorted(SIZES)
if embedded != expected:
    raise RuntimeError(f"Unexpected embedded sizes: {embedded}; expected: {expected}")

print(f"Created: {OUTPUT}")
print(f"Embedded sizes: {embedded}")
