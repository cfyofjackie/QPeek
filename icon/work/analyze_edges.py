from pathlib import Path

import numpy as np
from PIL import Image


root = Path(__file__).parent
source = np.asarray(Image.open(root / "image.png").convert("RGB"), dtype=np.int16)
edited = np.asarray(Image.open(root / "image-transparent-ai.png").convert("RGBA"), dtype=np.int16)

print("source", source.shape, "edited", edited.shape)
print("edited alpha", int(edited[..., 3].min()), int(edited[..., 3].max()), "transparent", int(np.sum(edited[..., 3] == 0)))

blue_strength = source[..., 2] - np.maximum(source[..., 0], source[..., 1])
for y in [0, 1, 2, 5, 10, 25, 50, 100, 150, 200, 240, 250, 300, 627, 1000, 1200, 1250, 1253]:
    xs = np.flatnonzero(blue_strength[y] > 20)
    extent = (int(xs[0]), int(xs[-1])) if xs.size else None
    print("row", y, "blue extent", extent)

for y, x0 in [(0, 200), (1, 200), (5, 200), (25, 150), (100, 50), (200, 5), (250, 0), (627, 0)]:
    print("samples row", y)
    for x in range(max(0, x0 - 4), min(source.shape[1], x0 + 9)):
        print(x, tuple(int(v) for v in source[y, x]), "b", int(blue_strength[y, x]), "ai-a", int(edited[y, x, 3]))
