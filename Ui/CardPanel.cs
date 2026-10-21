using System.Drawing.Drawing2D;

namespace MarkItDownDesktop.Ui;

/// <summary>Rounded card surface that paints its own background, gradient and border.</summary>
internal class CardPanel : Panel
{
    private Color fillColor = Color.White;
    private Color? fillToColor;
    private Color borderColor = Theme.CardBorder;
    private Color surroundColor = Theme.Page;
    private int radius = Theme.CardRadius;

    public CardPanel()
    {
        SetStyle(
            ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw,
            true);
        BackColor = Theme.Page;
    }

    public Color FillColor
    {
        get => fillColor;
        set { fillColor = value; Invalidate(); }
    }

    public Color? FillToColor
    {
        get => fillToColor;
        set { fillToColor = value; Invalidate(); }
    }

    public Color BorderColor
    {
        get => borderColor;
        set { borderColor = value; Invalidate(); }
    }

    /// <summary>Color painted outside the rounded corners, normally the page background.</summary>
    public Color SurroundColor
    {
        get => surroundColor;
        set { surroundColor = value; Invalidate(); }
    }

    public int Radius
    {
        get => radius;
        set { radius = value; Invalidate(); }
    }

    public bool ShowBorder { get; set; } = true;

    /// <summary>Path matching the area enclosed by the card border.</summary>
    public GraphicsPath CreateInnerPath() =>
        Theme.Rounded(new RectangleF(1f, 1f, Width - 3f, Height - 3f), Math.Max(1, Radius - 1));

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(SurroundColor);

        var rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
        using var path = Theme.Rounded(rect, Radius);
        if (FillToColor is { } to)
        {
            using var gradient = new LinearGradientBrush(new RectangleF(0f, 0f, Width, Math.Max(1, Height)), FillColor, to, 25f);
            g.FillPath(gradient, path);
        }
        else
        {
            using var fill = new SolidBrush(FillColor);
            g.FillPath(fill, path);
        }

        if (ShowBorder)
        {
            using var pen = new Pen(BorderColor);
            g.DrawPath(pen, path);
        }
    }
}
