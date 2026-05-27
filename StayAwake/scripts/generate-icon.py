"""Build app.ico and app-header.png from Assets/app-icon-source.png."""
from __future__ import annotations

from pathlib import Path

from PIL import Image

SIZES = (16, 24, 32, 48, 64, 128, 256)
ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Assets" / "app-icon-source.png"
OUT_ICO = ROOT / "Assets" / "app.ico"
OUT_HEADER = ROOT / "Assets" / "app-header.png"
OUT_DIR = ROOT / "Assets" / "icon-png"


def remove_near_black_background(im: Image.Image, threshold: int = 32) -> Image.Image:
    """Transparent header variant for dark UI (#141414)."""
    rgba = im.convert("RGBA")
    px = rgba.load()
    w, h = rgba.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if r <= threshold and g <= threshold and b <= threshold:
                px[x, y] = (0, 0, 0, 0)
    return rgba


def resize_icon(im: Image.Image, size: int) -> Image.Image:
    if size <= 32:
        return im.resize((size, size), Image.Resampling.LANCZOS)
    return im.resize((size, size), Image.Resampling.LANCZOS)


def main() -> None:
    if not SOURCE.exists():
        raise SystemExit(f"Missing source icon: {SOURCE}")

    source = Image.open(SOURCE).convert("RGBA")
    OUT_DIR.mkdir(parents=True, exist_ok=True)

    tray_images: list[Image.Image] = []
    for size in SIZES:
        frame = resize_icon(source, size)
        frame.save(OUT_DIR / f"stayawake-{size}.png")
        tray_images.append(frame)

    tray_images[-1].save(
        OUT_ICO,
        format="ICO",
        sizes=[(im.width, im.height) for im in tray_images],
        append_images=tray_images[:-1],
    )

    header = remove_near_black_background(source)
    header.resize((32, 32), Image.Resampling.LANCZOS).save(OUT_HEADER)

    print(f"Wrote {OUT_ICO} ({len(tray_images)} sizes)")
    print(f"Wrote {OUT_HEADER}")
    print(f"Optional PNGs: {OUT_DIR}")


if __name__ == "__main__":
    main()
