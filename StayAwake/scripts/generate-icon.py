"""Build app.ico, tray state ICOs, and app-header.png from Assets/app-icon-source.png."""
from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageEnhance

SIZES = (16, 24, 32, 48, 64, 128, 256)
ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Assets" / "app-icon-source.png"
OUT_ICO = ROOT / "Assets" / "app.ico"
OUT_HEADER = ROOT / "Assets" / "app-header.png"
OUT_DIR = ROOT / "Assets" / "icon-png"

TRAY_VARIANTS = {
    "disabled": ROOT / "Assets" / "app-tray-disabled.ico",
    "active": ROOT / "Assets" / "app-tray-active.ico",
    "completed": ROOT / "Assets" / "app-tray-completed.ico",
}

ACCENT_GREEN = (61, 214, 140, 255)
ACCENT_COMPLETED = (126, 184, 218, 255)


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
    return im.resize((size, size), Image.Resampling.LANCZOS)


def apply_tray_variant(frame: Image.Image, variant: str) -> Image.Image:
    """Return a copy styled for tray state (16px-readable, understated)."""
    out = frame.copy()
    size = out.size[0]

    if variant == "disabled":
        gray = ImageEnhance.Color(out).enhance(0.0)
        return ImageEnhance.Brightness(gray).enhance(0.52)

    if variant == "active":
        dot = max(2, size // 7)
        draw = ImageDraw.Draw(out)
        margin = max(0, size // 16)
        x1 = size - dot - margin
        y1 = size - dot - margin
        draw.ellipse((x1, y1, x1 + dot, y1 + dot), fill=ACCENT_GREEN)
        return out

    if variant == "completed":
        muted = ImageEnhance.Color(out).enhance(0.35)
        out = ImageEnhance.Brightness(muted).enhance(0.65)
        dot = max(2, size // 8)
        draw = ImageDraw.Draw(out)
        margin = max(0, size // 14)
        x1 = size - dot - margin
        y1 = margin
        draw.ellipse((x1, y1, x1 + dot, y1 + dot), fill=ACCENT_COMPLETED)
        return out

    return out


def write_ico(path: Path, frames: list[Image.Image]) -> None:
    frames[-1].save(
        path,
        format="ICO",
        sizes=[(im.width, im.height) for im in frames],
        append_images=frames[:-1],
    )


def main() -> None:
    if not SOURCE.exists():
        raise SystemExit(f"Missing source icon: {SOURCE}")

    source = Image.open(SOURCE).convert("RGBA")
    OUT_DIR.mkdir(parents=True, exist_ok=True)

    base_frames: list[Image.Image] = []
    for size in SIZES:
        frame = resize_icon(source, size)
        frame.save(OUT_DIR / f"stayawake-{size}.png")
        base_frames.append(frame)

    disabled_frames = [apply_tray_variant(f, "disabled") for f in base_frames]
    write_ico(OUT_ICO, disabled_frames)
    write_ico(TRAY_VARIANTS["disabled"], disabled_frames)

    for name in ("active", "completed"):
        variant_frames = [apply_tray_variant(f, name) for f in base_frames]
        write_ico(TRAY_VARIANTS[name], variant_frames)

    header = remove_near_black_background(source)
    header.resize((32, 32), Image.Resampling.LANCZOS).save(OUT_HEADER)

    print(f"Wrote {OUT_ICO} (neutral/disabled, {len(disabled_frames)} sizes)")
    for path in TRAY_VARIANTS.values():
        print(f"Wrote {path}")
    print(f"Wrote {OUT_HEADER}")
    print(f"Optional PNGs: {OUT_DIR}")


if __name__ == "__main__":
    main()
