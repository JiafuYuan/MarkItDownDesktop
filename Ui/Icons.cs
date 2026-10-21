using System.Drawing.Drawing2D;

namespace MarkItDownDesktop.Ui;

internal enum Glyph
{
    None,
    Plus,
    Trash,
    Refresh,
    Folder,
    More,
}

/// <summary>Vector icons drawn with GDI+ so the app ships without image assets.</summary>
internal static class Icons
{
    public static void Draw(Graphics g, Glyph glyph, RectangleF box, Color color)
    {
        var previous = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        try
        {
            switch (glyph)
            {
                case Glyph.Plus: Plus(g, box, color); break;
                case Glyph.Trash: Trash(g, box, color); break;
                case Glyph.Refresh: Refresh(g, box, color); break;
                case Glyph.Folder: Folder(g, box, color); break;
                case Glyph.More: More(g, box, color); break;
            }
        }
        finally
        {
            g.SmoothingMode = previous;
        }
    }

    private static void Plus(Graphics g, RectangleF box, Color color)
    {
        using var pen = RoundPen(color, Theme.S(1.3f));
        var cx = box.X + box.Width / 2f;
        var cy = box.Y + box.Height / 2f;
        var arm = Math.Min(box.Width, box.Height) / 2f - Theme.S(1f);
        g.DrawLine(pen, cx - arm, cy, cx + arm, cy);
        g.DrawLine(pen, cx, cy - arm, cx, cy + arm);
    }

    private static void Trash(Graphics g, RectangleF box, Color color)
    {
        using var pen = RoundPen(color, Theme.S(1.1f));
        var w = box.Width * 0.56f;
        var left = box.X + (box.Width - w) / 2f;
        var bodyTop = box.Y + box.Height * 0.30f;
        var bodyBottom = box.Y + box.Height * 0.88f;
        using (var body = Theme.Rounded(new RectangleF(left, bodyTop, w, bodyBottom - bodyTop), Theme.S(1.4f)))
        {
            g.DrawPath(pen, body);
        }

        g.DrawLine(pen, box.X + box.Width * 0.14f, bodyTop - box.Height * 0.10f, box.X + box.Width * 0.86f, bodyTop - box.Height * 0.10f);
        g.DrawLine(pen, box.X + box.Width * 0.40f, box.Y + box.Height * 0.12f, box.X + box.Width * 0.60f, box.Y + box.Height * 0.12f);
        g.DrawLine(pen, box.X + box.Width * 0.40f, bodyTop, box.X + box.Width * 0.40f, bodyBottom - box.Height * 0.12f);
        g.DrawLine(pen, box.X + box.Width * 0.60f, bodyTop, box.X + box.Width * 0.60f, bodyBottom - box.Height * 0.12f);
    }

    private static void Refresh(Graphics g, RectangleF box, Color color)
    {
        using var pen = RoundPen(color, Theme.S(1.2f));
        var rect = RectangleF.Inflate(box, -Theme.S(1f), -Theme.S(1f));
        g.DrawArc(pen, rect, 20f, 130f);
        g.DrawArc(pen, rect, 200f, 130f);
        ArrowHead(g, rect, 20f, color);
        ArrowHead(g, rect, 200f, color);
    }

    private static void ArrowHead(Graphics g, RectangleF rect, float startAngle, Color color)
    {
        const float sweep = 130f;
        var angle = (startAngle + sweep) * MathF.PI / 180f;
        var cx = rect.X + rect.Width / 2f;
        var cy = rect.Y + rect.Height / 2f;
        var rx = rect.Width / 2f;
        var ry = rect.Height / 2f;
        var px = cx + rx * MathF.Cos(angle);
        var py = cy + ry * MathF.Sin(angle);
        var tangent = MathF.Atan2(ry * MathF.Cos(angle), -rx * MathF.Sin(angle));
        var size = Theme.S(2.6f);
        using var brush = new SolidBrush(color);
        using var head = new GraphicsPath();
        head.AddPolygon(
        [
            new PointF(px + size * MathF.Cos(tangent), py + size * MathF.Sin(tangent)),
            new PointF(px + size * MathF.Cos(tangent + 2.5f), py + size * MathF.Sin(tangent + 2.5f)),
            new PointF(px + size * MathF.Cos(tangent - 2.5f), py + size * MathF.Sin(tangent - 2.5f)),
        ]);
        g.FillPath(brush, head);
    }

    private static void Folder(Graphics g, RectangleF box, Color color)
    {
        using var pen = RoundPen(color, Theme.S(1.1f));
        var w = box.Width * 0.78f;
        var h = box.Height * 0.62f;
        var left = box.X + (box.Width - w) / 2f;
        var top = box.Y + (box.Height - h) / 2f + Theme.S(1f);
        using var path = new GraphicsPath();
        path.AddLines(
        [
            new PointF(left, top + h),
            new PointF(left, top),
            new PointF(left + w * 0.42f, top),
            new PointF(left + w * 0.52f, top + h * 0.22f),
            new PointF(left + w, top + h * 0.22f),
            new PointF(left + w, top + h),
        ]);
        path.CloseFigure();
        g.DrawPath(pen, path);
    }

    private static void More(Graphics g, RectangleF box, Color color)
    {
        using var brush = new SolidBrush(color);
        var cx = box.X + box.Width / 2f;
        var r = Theme.S(1.1f);
        for (var i = -1; i <= 1; i++)
        {
            var cy = box.Y + box.Height / 2f + i * Theme.S(3.6f);
            g.FillEllipse(brush, cx - r, cy - r, r * 2f, r * 2f);
        }
    }

    /// <summary>Filled circle with a check mark, used for completed rows.</summary>
    public static void CheckCircle(Graphics g, RectangleF box, Color color)
    {
        var previous = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (var brush = new SolidBrush(color))
        {
            g.FillEllipse(brush, box);
        }

        using var pen = RoundPen(Color.White, Theme.S(1.2f));
        g.DrawLines(pen,
        [
            new PointF(box.X + box.Width * 0.27f, box.Y + box.Height * 0.52f),
            new PointF(box.X + box.Width * 0.44f, box.Y + box.Height * 0.69f),
            new PointF(box.X + box.Width * 0.74f, box.Y + box.Height * 0.33f),
        ]);
        g.SmoothingMode = previous;
    }

    /// <summary>Filled circle with an exclamation mark, used for failed rows.</summary>
    public static void AlertCircle(Graphics g, RectangleF box, Color color)
    {
        var previous = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (var brush = new SolidBrush(color))
        {
            g.FillEllipse(brush, box);
        }

        using var pen = RoundPen(Color.White, Theme.S(1.2f));
        g.DrawLine(pen, box.X + box.Width / 2f, box.Y + box.Height * 0.26f, box.X + box.Width / 2f, box.Y + box.Height * 0.58f);
        using (var brush = new SolidBrush(Color.White))
        {
            var r = Theme.S(0.8f);
            g.FillEllipse(brush, box.X + box.Width / 2f - r, box.Y + box.Height * 0.71f - r, r * 2f, r * 2f);
        }

        g.SmoothingMode = previous;
    }

    /// <summary>Empty circle used for rows that have not been converted yet.</summary>
    public static void EmptyCircle(Graphics g, RectangleF box, Color color)
    {
        var previous = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = RoundPen(color, Theme.S(1.1f));
        var rect = RectangleF.Inflate(box, -Theme.S(1f), -Theme.S(1f));
        g.DrawEllipse(pen, rect);
        g.SmoothingMode = previous;
    }

    /// <summary>Rounded square with a centred label, used as the file type badge in the list.</summary>
    public static void FileBadge(Graphics g, RectangleF box, string label, Color color)
    {
        var previous = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (var path = Theme.Rounded(box, Theme.S(4f)))
        using (var fill = new SolidBrush(Color.White))
        using (var pen = new Pen(color, Theme.S(0.9f)))
        {
            g.FillPath(fill, path);
            g.DrawPath(pen, path);
        }

        var font = label.Length switch
        {
            <= 1 => Theme.Badge,
            2 => Theme.BadgeMedium,
            _ => Theme.BadgeSmall,
        };

        using var brush = new SolidBrush(color);
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(label, font, brush, box, format);
        g.SmoothingMode = previous;
    }

    private static Pen RoundPen(Color color, float width) =>
        new(color, width) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round, Alignment = PenAlignment.Center };
}
