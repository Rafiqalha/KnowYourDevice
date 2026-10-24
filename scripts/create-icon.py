"""Render the repository's simple geometric app mark as Windows icon resources."""
from pathlib import Path
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parents[1]
scale = 8
image = Image.new('RGBA', (64 * scale, 64 * scale))
draw = ImageDraw.Draw(image)
draw.rounded_rectangle((0, 0, 64 * scale - 1, 64 * scale - 1), radius=17 * scale, fill='#b97559')
paths = [[(24, 16), (19, 16), (16, 19), (16, 24)], [(40, 16), (45, 16), (48, 19), (48, 24)], [(16, 40), (16, 45), (19, 48), (24, 48)], [(48, 40), (48, 45), (45, 48), (40, 48)], [(22, 33), (28, 33), (32, 25), (36, 40), (40, 33), (43, 33)]]
for points in paths:
    scaled = [(x * scale, y * scale) for x, y in points]
    draw.line(scaled, fill='#fffaf2', width=round(3.5 * scale), joint='curve')
    for x, y in (scaled[0], scaled[-1]):
        r = 1.75 * scale
        draw.ellipse((x-r, y-r, x+r, y+r), fill='#fffaf2')
resource = root / 'desktop' / 'resources'
public = root / 'desktop' / 'public'
public.mkdir(exist_ok=True)
image.save(resource / 'icon.ico', sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)])
image.resize((256, 256), Image.Resampling.LANCZOS).save(public / 'mark.png')
