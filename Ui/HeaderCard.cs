using System.Drawing.Drawing2D;

namespace MarkItDownDesktop.Ui;

/// <summary>Top card: application mark, titles, decorative artwork and the engine update button.</summary>
internal sealed class HeaderCard : CardPanel
{
    private const string TitleText = "MarkItDown";
    private const string LeadText = "拖放文件到这里，或点击此区域选择文件";
    private const string CaptionText = "支持多种文档格式，轻松转换为 Markdown 文本";

    private static readonly Color ArtworkText = Color.FromArgb(150, 186, 240);

    private readonly AccentButton updateButton = new();

    public HeaderCard()
    {
        FillColor = Theme.HeaderFrom;
        FillToColor = Theme.HeaderTo;
        SurroundColor = Theme.Page;
        Cursor = Cursors.Hand;
        AllowDrop = true;

        updateButton.Text = "检查引擎更新";
        updateButton.Icon = Glyph.Refresh;
        updateButton.HorizontalPadding = Theme.S(12);
        updateButton.IconSize = Theme.S(13);
        updateButton.IconGap = Theme.S(6);
        updateButton.MinimumHeight = Theme.S(34);
        updateButton.SizeChanged += (_, _) => PositionUpdateButton();
        Controls.Add(updateButton);
    }

    public AccentButton UpdateButton => updateButton;

    public event EventHandler? CardClicked;

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        PositionUpdateButton();
        Invalidate();
    }

    protected override void OnClick(EventArgs e)
    {
        CardClicked?.Invoke(this, EventArgs.Empty);
        base.OnClick(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var state = g.Save();
        using (var clip = CreateInnerPath())
        {
            g.SetClip(clip, CombineMode.Intersect);
            DrawWatermark(g);
            DrawArtwork(g);
        }

        g.Restore(state);

        DrawMark(g);
        DrawTexts(g);
    }

    private void PositionUpdateButton()
    {
        if (updateButton.Width <= 0 || Width <= 0) return;
        updateButton.Location = new Point(Math.Max(Theme.S(9), Width - updateButton.Width - Theme.S(7)), Theme.S(9));
    }

    /// <summary>Very light oversized mark behind the artwork.</summary>
    private void DrawWatermark(Graphics g)
    {
        using var font = new Font("Segoe UI", 86F, FontStyle.Bold);
        using var brush = new SolidBrush(Color.FromArgb(22, Theme.IllustrationDeep));
        g.DrawString("M\u2193", font, brush, new PointF(Width - Theme.S(190), -Theme.S(12)));
    }

    /// <summary>Document to markdown illustration shown on the right of the card.</summary>
    private void DrawArtwork(Graphics g)
    {
        var badgeSize = Theme.S(32);
        var sheetWidth = Theme.S(43);
        var sheetHeight = Theme.S(54);
        var arrowLength = Theme.S(23);
        var arrowGap = Theme.S(8);

        var right = Math.Max(Theme.S(330), updateButton.Width > 0 ? updateButton.Left - Theme.S(100) : Width - Theme.S(140));
        var badgeLeft = right - badgeSize;
        var arrowRight = badgeLeft - Theme.S(10);
        var arrowLeft = arrowRight - arrowLength;
        var sheet = new RectangleF(arrowLeft - arrowGap - sheetWidth, (Height - sheetHeight) / 2f, sheetWidth, sheetHeight);
        var centerY = Height / 2f;
        var badge = new RectangleF(badgeLeft, centerY - badgeSize / 2f, badgeSize, badgeSize);

        using (var path = Theme.Rounded(sheet, Theme.S(5f)))
        using (var fill = new SolidBrush(Theme.Illustration))
        {
            g.FillPath(fill, path);
        }

        using (var pen = new Pen(Color.White, Theme.S(2.6f)) { StartCap = LineCap.Round, EndCap = LineCap.Round })
        {
            for (var i = 0; i < 3; i++)
            {
                var y = sheet.Y + sheet.Height * 0.30f + i * sheet.Height * 0.19f;
                var lineRight = sheet.Right - (i == 2 ? Theme.S(16) : Theme.S(9));
                g.DrawLine(pen, sheet.X + Theme.S(9), y, lineRight, y);
            }
        }

        using (var pen = new Pen(Theme.IllustrationDeep, Theme.S(3.2f)) { StartCap = LineCap.Round, EndCap = LineCap.Round })
        {
            g.DrawLine(pen, arrowLeft, centerY, arrowRight, centerY);
        }

        var head = Theme.S(6f);
        using (var brush = new SolidBrush(Theme.IllustrationDeep))
        using (var path = new GraphicsPath())
        {
            path.AddPolygon(
            [
                new PointF(arrowRight + head, centerY),
                new PointF(arrowRight - Theme.S(1), centerY - head * 0.8f),
                new PointF(arrowRight - Theme.S(1), centerY + head * 0.8f),
            ]);
            g.FillPath(brush, path);
        }

        using (var path = Theme.Rounded(badge, Theme.S(8f)))
        using (var fill = new SolidBrush(Theme.Illustration))
        {
            g.FillPath(fill, path);
        }

        using (var brush = new SolidBrush(ArtworkText))
        using (var font = new Font("Segoe UI", 9.5F, FontStyle.Bold))
        using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
        {
            g.DrawString("M\u2193", font, brush, badge, format);
        }
    }

    /// <summary>Rounded blue app mark with a white document inside.</summary>
    private void DrawMark(Graphics g)
    {
        var size = Theme.S(57);
        var box = new RectangleF(Theme.S(13), (Height - size) / 2f, size, size);
        using (var path = Theme.Rounded(box, Theme.S(14f)))
        using (var fill = new SolidBrush(Theme.Accent))
        {
            g.FillPath(fill, path);
        }

        var sheet = new RectangleF(box.X + box.Width * 0.29f, box.Y + box.Height * 0.22f, box.Width * 0.41f, box.Height * 0.56f);
        using (var path = Theme.Rounded(sheet, Theme.S(3.5f)))
        using (var fill = new SolidBrush(Color.White))
        {
            g.FillPath(fill, path);
        }

        using (var pen = new Pen(Theme.Accent, Theme.S(2.6f)) { StartCap = LineCap.Round, EndCap = LineCap.Round })
        {
            g.DrawLine(pen, sheet.X + sheet.Width * 0.25f, sheet.Y + sheet.Height * 0.30f, sheet.Right - sheet.Width * 0.25f, sheet.Y + sheet.Height * 0.30f);
            g.DrawLine(pen, sheet.X + sheet.Width * 0.25f, sheet.Y + sheet.Height * 0.52f, sheet.Right - sheet.Width * 0.25f, sheet.Y + sheet.Height * 0.52f);
            g.DrawLine(pen, sheet.X + sheet.Width * 0.25f, sheet.Y + sheet.Height * 0.74f, sheet.X + sheet.Width * 0.58f, sheet.Y + sheet.Height * 0.74f);
        }
    }

    private void DrawTexts(Graphics g)
    {
        var title = TextRenderer.MeasureText(TitleText, Theme.Title, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
        var lead = TextRenderer.MeasureText(LeadText, Theme.Lead, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
        var caption = TextRenderer.MeasureText(CaptionText, Theme.Caption, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
        var total = title.Height + Theme.S(5) + lead.Height + Theme.S(4) + caption.Height;

        var left = Theme.S(94);
        var y = (Height - total) / 2;

        TextRenderer.DrawText(g, TitleText, Theme.Title, new Point(left, y), Theme.Navy, Flags);
        y += title.Height + Theme.S(5);
        TextRenderer.DrawText(g, LeadText, Theme.Lead, new Point(left, y), Theme.LeadColor, Flags);
        y += lead.Height + Theme.S(4);
        TextRenderer.DrawText(g, CaptionText, Theme.Caption, new Point(left, y), Theme.Muted, Flags);
    }

    private const TextFormatFlags Flags =
        TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
}
