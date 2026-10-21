using System.Drawing.Drawing2D;

namespace MarkItDownDesktop.Ui;

/// <summary>Rounded white field that hosts a read-only text box and a folder icon.</summary>
internal sealed class FieldBox : Panel
{
    private const int GlyphInset = 28;
    private readonly TextBox textBox = new();

    public FieldBox()
    {
        SetStyle(
            ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw,
            true);
        BackColor = Theme.SurfaceSubtle;
        Height = Theme.S(38);

        textBox.BorderStyle = BorderStyle.None;
        textBox.ReadOnly = true;
        textBox.BackColor = Color.White;
        textBox.ForeColor = Theme.Text;
        textBox.Font = Theme.Body;
        textBox.TabStop = false;
        textBox.Click += (_, _) => FieldClicked?.Invoke(this, EventArgs.Empty);
        Controls.Add(textBox);
    }

    public event EventHandler? FieldClicked;

    /// <summary>Color painted outside the rounded field, matching the surrounding card.</summary>
    public Color SurfaceColor { get; set; } = Theme.SurfaceSubtle;

    public string Value
    {
        get => textBox.Text;
        set => textBox.Text = value;
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        LayoutTextBox();
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        LayoutTextBox();
    }

    protected override void OnClick(EventArgs e)
    {
        FieldClicked?.Invoke(this, EventArgs.Empty);
        base.OnClick(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(SurfaceColor);

        var rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
        using var path = Theme.Rounded(rect, Theme.ControlRadius);
        using (var fill = new SolidBrush(Color.White))
        {
            g.FillPath(fill, path);
        }

        using (var pen = new Pen(Theme.FieldBorder))
        {
            g.DrawPath(pen, path);
        }

        const int glyph = 13;
        var glyphSize = Theme.S(glyph);
        Icons.Draw(g, Glyph.Folder, new RectangleF(Width - Theme.S(GlyphInset) + Theme.S(4), (Height - glyphSize) / 2f, glyphSize, glyphSize), Theme.Accent);
    }

    private void LayoutTextBox()
    {
        if (Width <= 0 || Height <= 0) return;

        textBox.Width = Math.Max(Theme.S(10), Width - Theme.S(14) - Theme.S(GlyphInset));
        textBox.Location = new Point(Theme.S(11), (Height - textBox.Height) / 2);
    }
}
