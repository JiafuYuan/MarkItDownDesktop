using System.Drawing.Drawing2D;

namespace MarkItDownDesktop.Ui;

/// <summary>Status text with a leading state icon, drawn on the surrounding card surface.</summary>
internal sealed class StatusLine : Control
{
    private State state = State.Idle;

    public StatusLine()
    {
        SetStyle(
            ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw,
            true);
        Height = Theme.S(20);
        Font = Theme.Status;
        BackColor = Theme.SurfaceSubtle;
    }

    public enum State
    {
        Idle,
        Success,
        Failure,
    }

    public State Current
    {
        get => state;
        set { state = value; Invalidate(); }
    }

    /// <summary>Color painted outside the drawn content, matching the surrounding card.</summary>
    public Color SurfaceColor { get; set; } = Theme.SurfaceSubtle;

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(SurfaceColor);

        const int iconSize = 14;
        var size = Theme.S(iconSize);
        var box = new RectangleF(0f, (Height - size) / 2f, size, size);
        switch (state)
        {
            case State.Success:
                Icons.CheckCircle(g, box, Theme.Success);
                break;
            case State.Failure:
                Icons.AlertCircle(g, box, Theme.Failure);
                break;
            default:
                Icons.EmptyCircle(g, box, Theme.Pending);
                break;
        }

        var left = (int)box.Right + Theme.S(7);
        TextRenderer.DrawText(
            g,
            Text,
            Font,
            new Rectangle(left, 0, Math.Max(1, Width - left), Height),
            state == State.Failure ? Theme.Failure : Theme.Text,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
    }
}
