using System.Drawing.Drawing2D;

namespace MarkItDownDesktop.Ui;

/// <summary>Rounded button with an optional vector icon, in a filled or outlined variant.</summary>
internal sealed class AccentButton : Button
{
    private bool hovered;
    private bool pressed;

    public AccentButton()
    {
        SetStyle(
            ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw,
            true);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Font = Theme.ButtonText;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Cursor = Cursors.Hand;
        UseVisualStyleBackColor = false;
        TabStop = false;
    }

    /// <summary>Filled accent variant when true, outlined variant when false.</summary>
    public bool Primary { get; set; } = true;

    public Glyph Icon { get; set; } = Glyph.None;

    public int HorizontalPadding { get; set; } = Theme.S(13);

    public int IconSize { get; set; } = Theme.S(14);

    public int IconGap { get; set; } = Theme.S(7);

    /// <summary>Height the button grows to when the text alone would be shorter.</summary>
    public int MinimumHeight { get; set; } = Theme.S(34);

    public override Size GetPreferredSize(Size proposedSize)
    {
        var text = TextRenderer.MeasureText(Text, Font);
        var width = text.Width + HorizontalPadding * 2 + (Icon == Glyph.None ? 0 : IconSize + IconGap);
        var height = Math.Max(text.Height + Theme.S(12), MinimumHeight);
        return new Size(width, height);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyRoundedRegion();
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        ApplyRoundedRegion();
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        hovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        hovered = false;
        pressed = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        pressed = true;
        Invalidate();
        base.OnMouseDown(mevent);
    }

    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        pressed = false;
        Invalidate();
        base.OnMouseUp(mevent);
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        Cursor = Enabled ? Cursors.Hand : Cursors.Default;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var background = BackgroundColor();
        using (var path = Theme.Rounded(new RectangleF(0f, 0f, Width, Height), Theme.ControlRadius))
        {
            using (var fill = new SolidBrush(background))
            {
                g.FillPath(fill, path);
            }

            if (!Primary)
            {
                using var pen = new Pen(Enabled ? Theme.Accent : Theme.CheckboxBorder);
                g.DrawPath(pen, path);
            }
        }

        var foreground = ForegroundColor();
        var textWidth = TextRenderer.MeasureText(Text, Font).Width;
        var contentWidth = textWidth + (Icon == Glyph.None ? 0 : IconSize + IconGap);
        var x = (Width - contentWidth) / 2f;

        if (Icon != Glyph.None)
        {
            Icons.Draw(g, Icon, new RectangleF(x, (Height - IconSize) / 2f, IconSize, IconSize), foreground);
            x += IconSize + IconGap;
        }

        TextRenderer.DrawText(
            g,
            Text,
            Font,
            new Rectangle((int)x, 0, Math.Max(1, Width - (int)x), Height),
            foreground,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
    }

    private Color BackgroundColor()
    {
        if (!Enabled) return Color.FromArgb(238, 241, 246);
        if (Primary) return pressed ? Theme.AccentPressed : hovered ? Theme.AccentHover : Theme.Accent;
        return pressed ? Theme.AccentSoft : hovered ? Theme.AccentSoft : Color.White;
    }

    private Color ForegroundColor()
    {
        if (!Enabled) return Theme.Muted;
        return Primary ? Color.White : Theme.Accent;
    }

    private void ApplyRoundedRegion()
    {
        if (Width <= 0 || Height <= 0) return;
        using var path = Theme.Rounded(new RectangleF(0f, 0f, Width, Height), Theme.ControlRadius);
        var previous = Region;
        Region = new Region(path);
        previous?.Dispose();
    }
}
