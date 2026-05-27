"""Renders a tray context menu preview matching TrayIconManager labels (for README)."""
from pathlib import Path

try:
    from PIL import Image, ImageDraw, ImageFont
except ImportError:
    raise SystemExit("pip install pillow")

OUT = Path(__file__).resolve().parents[2] / "docs" / "screenshots" / "tray-menu.png"
W, H = 240, 280
BG = (28, 28, 28)
BORDER = (51, 51, 51)
TEXT = (243, 243, 243)
MUTED = (160, 160, 160)
DISABLED = (102, 102, 102)
HIGHLIGHT = (26, 61, 46)
GREEN = (61, 214, 140)

# Menu while a session is active (presets grayed, Stop session available)
ITEMS = [
    ("● Active", False, "status"),
    ("Open settings", True, "normal"),
    ("—", False, "sep"),
    ("Start 30 minutes", False, "muted"),
    ("Start 1 hour", False, "muted"),
    ("Start 3 hours", False, "muted"),
    ("Start indefinitely", False, "muted"),
    ("—", False, "sep"),
    ("Stop session", True, "normal"),
    ("—", False, "sep"),
    ("Exit", True, "normal"),
]

img = Image.new("RGB", (W, H), BG)
draw = ImageDraw.Draw(img)
draw.rectangle([0, 0, W - 1, H - 1], outline=BORDER)

try:
    font = ImageFont.truetype("segoeui.ttf", 13)
except OSError:
    font = ImageFont.load_default()

y = 8
for label, enabled, kind in ITEMS:
    if kind == "sep":
        draw.line([(8, y + 6), (W - 8, y + 6)], fill=BORDER)
        y += 14
        continue

    if kind == "status":
        color = GREEN
    elif kind == "muted":
        color = MUTED
    else:
        color = TEXT if enabled else MUTED

    draw.text((12, y), label, fill=color, font=font)
    y += 22

OUT.parent.mkdir(parents=True, exist_ok=True)
img.save(OUT)
print(f"Wrote {OUT}")
