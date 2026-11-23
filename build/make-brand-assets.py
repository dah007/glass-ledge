"""Regenerates GlassLedge's icon and installer images from code (needs Pillow: pip install pillow).

Writes: src/GlassLedge.App/Assets/app.ico + app-256.png, docs/assets/icon-256.png,
installer/wizard-164x314.bmp, wizard-328x628.bmp, wizard-small-55.bmp, wizard-small-110.bmp,
the Microsoft Store package logos in packaging/msix/Assets, and the Store listing art in packaging/store/art.
Run from the repo root:  python build/make-brand-assets.py
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parent.parent
S = 1024
AMBER_TOP, AMBER_BOT = (0xFF, 0xC2, 0x4D), (0xFF, 0xB0, 0x20)
UMBER = (0x8A, 0x53, 0x00, 240)
HAZE = (157, 189, 224)


def lerp(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


def grad(w, h, top, bot):
    col = Image.new("RGBA", (1, h))
    for y in range(h):
        col.putpixel((0, y), lerp(top, bot, y / (h - 1)) + (255,))
    return col.resize((w, h))


def rmask(size, box, r):
    m = Image.new("L", size, 0)
    ImageDraw.Draw(m).rounded_rectangle(box, r, fill=255)
    return m


def layer(size=(S, S)):
    return Image.new("RGBA", size, (0, 0, 0, 0))


def shape(base, box, r, rgba):
    l = layer(base.size)
    ImageDraw.Draw(l).rounded_rectangle(box, r, fill=rgba)
    return Image.alpha_composite(base, l)


def stroke(base, box, r, rgba, w):
    l = layer(base.size)
    ImageDraw.Draw(l).rounded_rectangle(box, r, outline=rgba, width=w)
    return Image.alpha_composite(base, l)


def folder(base, cx, cy, sz, color):
    w, h = sz, int(sz * 0.72)
    x0, y0 = cx - w // 2, cy - h // 2
    l = layer(base.size)
    d = ImageDraw.Draw(l)
    d.rounded_rectangle((x0, y0 - int(sz * 0.12), x0 + int(w * 0.42), y0 + int(sz * 0.2)), int(sz * 0.06), fill=color)
    d.rounded_rectangle((x0, y0, x0 + w, y0 + h), int(sz * 0.1), fill=color)
    return Image.alpha_composite(base, l)


def plus(base, cx, cy, sz, color, wd):
    l = layer(base.size)
    d = ImageDraw.Draw(l)
    d.rounded_rectangle((cx - sz, cy - wd // 2, cx + sz, cy + wd // 2), wd // 2, fill=color)
    d.rounded_rectangle((cx - wd // 2, cy - sz, cx + wd // 2, cy + sz), wd // 2, fill=color)
    return Image.alpha_composite(base, l)


def icon() -> Image.Image:
    """Mono Grid on a Dusk pane: amber folder tile, two glass tiles, an add tile."""
    pad, radius = 48, 230
    box = (pad, pad, S - pad, S - pad)
    mask = rmask((S, S), box, radius)
    base = layer()
    base.paste(grad(S, S, (0x2A, 0x30, 0x58), (0x14, 0x17, 0x2B)), (0, 0), mask)
    gloss = layer()
    ImageDraw.Draw(gloss).ellipse((pad - 260, pad - 600, S - pad + 260, pad + 300), fill=(255, 255, 255, 30))
    gloss = gloss.filter(ImageFilter.GaussianBlur(40))
    gloss.putalpha(Image.composite(gloss.getchannel("A"), Image.new("L", (S, S), 0), mask))
    base = Image.alpha_composite(base, gloss)
    base = stroke(base, box, radius, HAZE + (95,), 6)
    t, gap = 330, 56
    x0 = (S - (2 * t + gap)) // 2
    tiles = [(x0, x0), (x0 + t + gap, x0), (x0, x0 + t + gap), (x0 + t + gap, x0 + t + gap)]
    for i, (x, y) in enumerate(tiles):
        if i == 0:
            glow = layer()
            ImageDraw.Draw(glow).rounded_rectangle((x - 6, y - 6, x + t + 6, y + t + 6), 98, fill=(255, 176, 32, 110))
            base = Image.alpha_composite(base, glow.filter(ImageFilter.GaussianBlur(26)))
            tile_mask = Image.new("L", (t, t), 0)
            ImageDraw.Draw(tile_mask).rounded_rectangle((0, 0, t - 1, t - 1), 90, fill=255)
            base.paste(grad(t, t, AMBER_TOP, AMBER_BOT), (x, y), tile_mask)
            base = folder(base, x + t // 2, y + t // 2 + 8, int(t * 0.56), UMBER)
        else:
            base = shape(base, (x, y, x + t, y + t), 90, HAZE + (58,))
            base = stroke(base, (x, y, x + t, y + t), 90, HAZE + (80,), 3)
            if i == 3:
                base = plus(base, x + t // 2, y + t // 2, int(t * 0.2), (236, 239, 244, 240), 30)
    return base


def wizard(ic: Image.Image, w=328, h=628) -> Image.Image:
    """Tall installer banner: Dusk sky with an amber glow, a glass pane holding the icon, the wordmark."""
    bg = grad(w, h, (0x3A, 0x3F, 0x6B), (0x1B, 0x1F, 0x36))
    glow = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    ImageDraw.Draw(glow).ellipse((w * 0.2, -h * 0.18, w * 1.3, h * 0.2), fill=(255, 176, 32, 70))
    bg = Image.alpha_composite(bg, glow.filter(ImageFilter.GaussianBlur(50)))
    pane = (28, 140, w - 28, 460)
    bg = shape(bg, pane, 64, (255, 255, 255, 34))
    bg = stroke(bg, pane, 64, HAZE + (150,), 2)
    side = 180
    small = ic.crop((48, 48, S - 48, S - 48)).resize((side, side), Image.LANCZOS)
    bg.alpha_composite(small, ((w - side) // 2, 210))
    font = None
    for name in ("DejaVuSans-Bold.ttf", "arialbd.ttf", "segoeuib.ttf"):
        try:
            font = ImageFont.truetype(name, 40)
            break
        except OSError:
            continue
    font = font or ImageFont.load_default()
    d = ImageDraw.Draw(bg)
    tw = d.textlength("GlassLedge", font=font)
    d.text(((w - tw) / 2, 500), "GlassLedge", font=font, fill=(255, 255, 255, 255))
    return bg.convert("RGB")


def small_image(ic: Image.Image, px: int) -> Image.Image:
    return ic.crop((48, 48, S - 48, S - 48)).resize((px, px), Image.LANCZOS)


def bold_font(size: int):
    for name in ("DejaVuSans-Bold.ttf", "arialbd.ttf", "segoeuib.ttf"):
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            continue
    return ImageFont.load_default()


def regular_font(size: int):
    for name in ("DejaVuSans.ttf", "arial.ttf", "segoeui.ttf"):
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            continue
    return ImageFont.load_default()


def fit_font(text: str, max_width: float, start: int, bold: bool = True):
    """The largest font, no bigger than `start`, that draws `text` within `max_width`."""
    probe = ImageDraw.Draw(Image.new("RGB", (1, 1)))
    size = start
    while size > 8:
        font = bold_font(size) if bold else regular_font(size)
        if probe.textlength(text, font=font) <= max_width:
            return font
        size -= 2
    return bold_font(8)


def brand_sky(w: int, h: int) -> Image.Image:
    """Dusk gradient with a warm amber glow near the top: the background for Store art and tiles."""
    bg = grad(w, h, (0x3A, 0x3F, 0x6B), (0x1B, 0x1F, 0x36))
    glow = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    ImageDraw.Draw(glow).ellipse((w * 0.15, -h * 0.3, w * 1.2, h * 0.25), fill=(255, 176, 32, 62))
    return Image.alpha_composite(bg, glow.filter(ImageFilter.GaussianBlur(max(w, h) // 14)))


def centered_text(img: Image.Image, text: str, y: int, font, fill) -> None:
    d = ImageDraw.Draw(img)
    d.text(((img.width - d.textlength(text, font=font)) / 2, y), text, font=font, fill=fill)


def mark(ic: Image.Image, px: int) -> Image.Image:
    """The icon trimmed of its transparent margin and scaled to a square."""
    return ic.crop((48, 48, S - 48, S - 48)).resize((px, px), Image.LANCZOS)


def store_package_logos(ic: Image.Image) -> None:
    """Logos the MSIX manifest points at, in the scale and target-size forms Windows picks from."""
    out = ROOT / "packaging/msix/Assets"
    out.mkdir(parents=True, exist_ok=True)

    def save(name: str, im: Image.Image) -> None:
        im.save(out / name)

    for scale, k in ((100, 1), (200, 2), (400, 4)):
        save(f"Square44x44Logo.scale-{scale}.png", mark(ic, 44 * k))
        save(f"Square150x150Logo.scale-{scale}.png", mark(ic, 150 * k))
        save(f"StoreLogo.scale-{scale}.png", mark(ic, 50 * k))
        w, h = 310 * k, 150 * k
        wide = brand_sky(w, h)
        side = round(h * 0.7)
        wide.alpha_composite(mark(ic, side), (round(h * 0.18), (h - side) // 2))
        text_x = round(h * 0.18) + side + round(h * 0.12)
        label = fit_font("GlassLedge", w - text_x - round(h * 0.1), round(h * 0.2))
        ImageDraw.Draw(wide).text((text_x, round(h * 0.38)), "GlassLedge", font=label, fill=(255, 255, 255, 255))
        save(f"Wide310x150Logo.scale-{scale}.png", wide.convert("RGBA"))
    for size in (16, 24, 32, 48, 256):
        im = mark(ic, size)
        save(f"Square44x44Logo.targetsize-{size}.png", im)
        save(f"Square44x44Logo.altform-unplated_targetsize-{size}.png", im)


def store_listing_art(ic: Image.Image) -> None:
    """Optional Store listing images: 1:1 tile icon, 1:1 box art, 2:3 poster art, 16:9 super hero art."""
    out = ROOT / "packaging/store/art"
    out.mkdir(parents=True, exist_ok=True)
    tagline = "Your favorite folders, floating on your desktop"

    mark(ic, 300).save(out / "tile-icon-300x300.png")

    box = brand_sky(1080, 1080)
    box.alpha_composite(mark(ic, 640), (220, 150))
    centered_text(box, "GlassLedge", 830, bold_font(96), (255, 255, 255, 255))
    box.convert("RGB").save(out / "box-art-1080x1080.png")

    poster = brand_sky(720, 1080)
    poster.alpha_composite(mark(ic, 480), (120, 190))
    centered_text(poster, "GlassLedge", 730, bold_font(76), (255, 255, 255, 255))
    centered_text(poster, "Glass folder widgets", 840, regular_font(34), (236, 239, 244, 255))
    centered_text(poster, "for your Windows 11 desktop", 888, regular_font(34), (236, 239, 244, 255))
    poster.convert("RGB").save(out / "poster-art-720x1080.png")

    hero = brand_sky(1920, 1080)
    hero.alpha_composite(mark(ic, 520), (230, 280))
    d = ImageDraw.Draw(hero)
    d.text((860, 380), "GlassLedge", font=fit_font("GlassLedge", 1000, 150), fill=(255, 255, 255, 255))
    d.text((866, 590), tagline, font=fit_font(tagline, 1920 - 866 - 90, 48, bold=False), fill=(236, 239, 244, 255))
    hero.convert("RGB").save(out / "super-hero-1920x1080.png")


def main() -> None:
    ic = icon()
    ic256 = ic.resize((256, 256), Image.LANCZOS)
    ic256.save(ROOT / "src/GlassLedge.App/Assets/app-256.png")
    ic256.save(ROOT / "docs/assets/icon-256.png")
    sizes = [(s, s) for s in (16, 20, 24, 32, 40, 48, 64, 128, 256)]
    ic.save(ROOT / "src/GlassLedge.App/Assets/app.ico", sizes=sizes)
    big = wizard(ic)
    big.save(ROOT / "installer/wizard-328x628.bmp")
    big.resize((164, 314), Image.LANCZOS).save(ROOT / "installer/wizard-164x314.bmp")
    for px in (110, 55):
        flat = Image.new("RGBA", (px, px), (255, 255, 255, 255))  # the wizard header is white
        inner = round(px * 0.74)  # Setup draws this image flush to the window's top-right, so leave a margin
        offset = (px - inner) // 2
        flat.alpha_composite(small_image(ic, inner), (offset, offset))
        flat.convert("RGB").save(ROOT / f"installer/wizard-small-{px}.bmp")
    store_package_logos(ic)
    store_listing_art(ic)


if __name__ == "__main__":
    main()
