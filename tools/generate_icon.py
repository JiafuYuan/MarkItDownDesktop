from pathlib import Path

from PIL import Image, ImageDraw


def draw_icon(size: int) -> Image.Image:
    scale = 4
    canvas_size = size * scale
    image = Image.new("RGBA", (canvas_size, canvas_size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)

    def xy(value: float) -> int:
        return round(value * scale)

    # Rounded navy tile, matching the desktop UI accent.
    margin = max(2, size * 0.035)
    draw.rounded_rectangle(
        (xy(margin), xy(margin), xy(size - margin), xy(size - margin)),
        radius=xy(size * 0.20),
        fill=(20, 51, 82, 255),
    )

    # White document with a folded top-right corner.
    left, top = size * 0.25, size * 0.16
    right, bottom = size * 0.75, size * 0.84
    fold = size * 0.18
    paper = [(xy(left), xy(top)), (xy(right - fold), xy(top)),
             (xy(right), xy(top + fold)), (xy(right), xy(bottom)),
             (xy(left), xy(bottom))]
    draw.polygon(paper, fill=(248, 251, 253, 255))
    draw.polygon(
        [(xy(right - fold), xy(top)), (xy(right - fold), xy(top + fold)),
         (xy(right), xy(top + fold))],
        fill=(179, 207, 228, 255),
    )

    # Markdown-style text lines.
    line_color = (111, 151, 181, 255)
    for y, width in ((0.39, 0.29), (0.49, 0.37), (0.59, 0.24)):
        draw.rounded_rectangle(
            (xy(left + size * 0.10), xy(size * y),
             xy(left + size * (0.10 + width)), xy(size * (y + 0.045))),
            radius=xy(size * 0.02),
            fill=line_color,
        )

    # Down arrow: conversion/export cue.
    arrow_color = (28, 126, 214, 255)
    shaft_left = size * 0.45
    shaft_right = size * 0.55
    draw.rounded_rectangle(
        (xy(shaft_left), xy(size * 0.55), xy(shaft_right), xy(size * 0.73)),
        radius=xy(size * 0.04),
        fill=arrow_color,
    )
    draw.polygon(
        [(xy(size * 0.36), xy(size * 0.69)),
         (xy(size * 0.64), xy(size * 0.69)),
         (xy(size * 0.50), xy(size * 0.84))],
        fill=arrow_color,
    )

    return image.resize((size, size), Image.Resampling.LANCZOS)


if __name__ == "__main__":
    output = Path(__file__).resolve().parents[1] / "app.ico"
    images = [draw_icon(size) for size in (256, 128, 64, 48, 32, 16)]
    images[0].save(output, format="ICO", sizes=[(image.width, image.height) for image in images], append_images=images[1:])
    print(output)
