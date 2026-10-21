using System.Drawing.Drawing2D;

namespace MarkItDownDesktop.Ui;

/// <summary>Thick rounded progress bar.</summary>
internal sealed class RoundedProgressBar : Control
{
    private int minimum;
    private int maximum = 100;
    private int value;
    private Color fillColor = Theme.ProgressFill;

    public RoundedProgressBar()
    {
        SetStyle(
            ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw,
            true);
        Height = Theme.S(9);
        BackColor = Theme.Page;
    }

    public int Minimum
    {
        get => minimum;
        set { minimum = value; Invalidate(); }
    }

    public int Maximum
    {
        get => maximum;
        set { maximum = Math.Max(1, value); Invalidate(); }
    }

    public int Value
    {
        get => value;
        set { this.value = Math.Clamp(value, minimum, maximum); Invalidate(); }
    }

    public Color FillColor
    {
        get => fillColor;
        set { fillColor = value; Invalidate(); }
    }

    /// <summary>Color painted outside the rounded bar, normally the surrounding card.</summary>
    public Color SurroundColor { get; set; } = Theme.Page;

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        if (Width <= 0 || Height <= 0) return;
        using var path = Theme.Rounded(new RectangleF(0f, 0f, Width, Height), Height / 2f);
        var previous = Region;
        Region = new Region(path);
        previous?.Dispose();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(SurroundColor);

        var bounds = new RectangleF(0f, 0f, Width, Height);
        using (var track = Theme.Rounded(bounds, Height / 2f))
        using (var brush = new SolidBrush(Theme.ProgressTrack))
        {
            g.FillPath(brush, track);
        }

        var span = maximum - minimum;
        if (span <= 0 || value <= minimum) return;

        var ratio = Math.Clamp((value - minimum) / (float)span, 0f, 1f);
        using var clip = Theme.Rounded(bounds, Height / 2f);
        var previous = g.Clip;
        g.SetClip(clip);
        using (var brush = new SolidBrush(FillColor))
        {
            g.FillRectangle(brush, bounds.X, bounds.Y, Math.Max(Height, Width * ratio), Height);
        }

        g.Clip = previous;
    }
}
