from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw


ROOT = Path(__file__).parent
SOURCE = ROOT / "image.png"
PNG_OUTPUT = ROOT.parents[0] / "outputs" / "QPeek-transparent.png"
ICO_OUTPUT = ROOT.parents[0] / "outputs" / "QPeek.ico"
PREVIEW_OUTPUT = ROOT.parents[0] / "outputs" / "QPeek-tray-preview-dark.png"
SIZES = [16, 24, 32, 48, 64, 128, 256]


def alpha_from_white_composite(observed: np.ndarray, foreground: np.ndarray) -> float:
    """Estimate alpha assuming observed = alpha*foreground + (1-alpha)*white."""
    bg_distance = 255.0 - foreground.astype(np.float64)
    observed_distance = 255.0 - observed.astype(np.float64)
    denominator = float(np.dot(bg_distance, bg_distance))
    if denominator < 1.0:
        return 0.0
    return float(np.clip(np.dot(observed_distance, bg_distance) / denominator, 0.0, 1.0))


source_image = Image.open(SOURCE).convert("RGB")
source = np.asarray(source_image, dtype=np.uint8)
height, width = source.shape[:2]
if width != height:
    raise RuntimeError(f"Expected a square source image, got {width}x{height}")

source_i16 = source.astype(np.int16)
blue_strength = source_i16[..., 2] - np.maximum(source_i16[..., 0], source_i16[..., 1])
solid_blue = blue_strength > 100

alpha = np.zeros((height, width), dtype=np.uint8)
result_rgb = source.copy()


def apply_line(line_pixels: np.ndarray, line_solid: np.ndarray, coordinates: list[tuple[int, int]]) -> None:
    solid_indices = np.flatnonzero(line_solid)
    if solid_indices.size == 0:
        return

    first = int(solid_indices[0])
    last = int(solid_indices[-1])
    for index in range(first, last + 1):
        y, x = coordinates[index]
        alpha[y, x] = 255

    left_foreground = np.mean(line_pixels[first : min(first + 5, len(line_pixels))], axis=0)
    for index in range(first - 1, -1, -1):
        strength = int(line_pixels[index, 2]) - max(int(line_pixels[index, 0]), int(line_pixels[index, 1]))
        candidate = alpha_from_white_composite(line_pixels[index], left_foreground)
        if candidate < 0.015 or strength <= 1:
            break
        y, x = coordinates[index]
        candidate_alpha = int(round(candidate * 255))
        if candidate_alpha > alpha[y, x]:
            alpha[y, x] = candidate_alpha
            recovered = (line_pixels[index].astype(np.float64) - 255.0 * (1.0 - candidate)) / candidate
            result_rgb[y, x] = np.clip(np.rint(recovered), 0, 255).astype(np.uint8)

    right_foreground = np.mean(line_pixels[max(0, last - 4) : last + 1], axis=0)
    for index in range(last + 1, len(line_pixels)):
        strength = int(line_pixels[index, 2]) - max(int(line_pixels[index, 0]), int(line_pixels[index, 1]))
        candidate = alpha_from_white_composite(line_pixels[index], right_foreground)
        if candidate < 0.015 or strength <= 1:
            break
        y, x = coordinates[index]
        candidate_alpha = int(round(candidate * 255))
        if candidate_alpha > alpha[y, x]:
            alpha[y, x] = candidate_alpha
            recovered = (line_pixels[index].astype(np.float64) - 255.0 * (1.0 - candidate)) / candidate
            result_rgb[y, x] = np.clip(np.rint(recovered), 0, 255).astype(np.uint8)


for y in range(height):
    apply_line(source[y], solid_blue[y], [(y, x) for x in range(width)])

for x in range(width):
    apply_line(source[:, x], solid_blue[:, x], [(y, x) for y in range(height)])

# Preserve every fully opaque source pixel exactly. Hidden RGB is set to a neutral
# blue so high-quality resampling cannot introduce dark or white fringes.
result_rgb[alpha == 0] = np.array([0, 101, 248], dtype=np.uint8)
rgba = np.dstack((result_rgb, alpha))
transparent_source = Image.fromarray(rgba, "RGBA")
PNG_OUTPUT.parent.mkdir(parents=True, exist_ok=True)
transparent_source.save(PNG_OUTPUT, "PNG", optimize=True)


def resize_premultiplied(image: Image.Image, size: int) -> Image.Image:
    return image.convert("RGBa").resize((size, size), Image.Resampling.LANCZOS).convert("RGBA")


frames = [resize_premultiplied(transparent_source, size) for size in SIZES]
largest = frames[-1]
largest.save(
    ICO_OUTPUT,
    "ICO",
    sizes=[(size, size) for size in SIZES],
    append_images=frames[:-1],
    bitmap_format="png",
)

with Image.open(ICO_OUTPUT) as icon:
    embedded = sorted(icon.ico.sizes())
expected = [(size, size) for size in SIZES]
if embedded != expected:
    raise RuntimeError(f"Unexpected ICO sizes: {embedded}; expected {expected}")

# Tray-focused QA preview: real-size icons above and nearest-neighbor magnification below.
preview = Image.new("RGB", (520, 340), "#202020")
draw = ImageDraw.Draw(preview)
draw.text((20, 16), "Dark taskbar / tray simulation — actual size", fill="#f0f0f0")
for icon_frame, size, x in [(frames[0], 16, 120), (frames[1], 24, 330)]:
    preview.paste(icon_frame, (x, 58), icon_frame)
    draw.text((x - 16, 94), f"{size} x {size}", fill="#bfbfbf")

draw.text((20, 135), "8x pixel inspection", fill="#f0f0f0")
for icon_frame, size, x in [(frames[0], 16, 55), (frames[1], 24, 285)]:
    magnified = icon_frame.resize((size * 8, size * 8), Image.Resampling.NEAREST)
    preview.paste(magnified, (x, 170), magnified)
preview.save(PREVIEW_OUTPUT, "PNG")

print(f"Transparent PNG: {PNG_OUTPUT}")
print(f"ICO: {ICO_OUTPUT}")
print(f"Embedded sizes: {embedded}")
print(f"Alpha range: {int(alpha.min())}..{int(alpha.max())}")
print(f"Fully transparent pixels: {int(np.sum(alpha == 0))}")
print(f"Fully opaque pixels: {int(np.sum(alpha == 255))}")
print(f"Tray preview: {PREVIEW_OUTPUT}")
