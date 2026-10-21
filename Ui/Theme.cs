using System.Drawing.Drawing2D;

namespace MarkItDownDesktop.Ui;

/// <summary>
/// Colors, fonts and geometry shared by the custom drawn interface.
/// Geometry is expressed in 96 DPI units and scaled through <see cref="S(int)"/>
/// so that the layout keeps its proportions on high DPI displays, where fonts
/// are rendered at the device resolution automatically.
/// </summary>
internal static class Theme
{
    /// <summary>Device pixels per 96 DPI unit.</summary>
    public static float Scale { get; set; } = 1f;

    public static int S(int value) => (int)Math.Round(value * Scale, MidpointRounding.AwayFromZero);

    public static float S(float value) => value * Scale;

    public static Size S(int width, int height) => new(S(width), S(height));

    public static readonly Color Page = Color.FromArgb(246, 248, 251);
    public static readonly Color Accent = Color.FromArgb(46, 125, 247);
    public static readonly Color AccentHover = Color.FromArgb(28, 106, 226);
    public static readonly Color AccentPressed = Color.FromArgb(20, 88, 197);
    public static readonly Color AccentSoft = Color.FromArgb(232, 241, 254);
    public static readonly Color Navy = Color.FromArgb(23, 55, 94);
    public static readonly Color Text = Color.FromArgb(36, 59, 83);
    public static readonly Color Muted = Color.FromArgb(139, 156, 178);
    public static readonly Color CardBorder = Color.FromArgb(228, 234, 242);
    public static readonly Color HeaderFrom = Color.FromArgb(250, 252, 255);
    public static readonly Color HeaderTo = Color.FromArgb(228, 240, 254);
    public static readonly Color SurfaceSubtle = Color.FromArgb(247, 249, 252);
    public static readonly Color Divider = Color.FromArgb(235, 240, 246);
    public static readonly Color RowHover = Color.FromArgb(244, 248, 254);
    public static readonly Color RowSelected = Color.FromArgb(233, 242, 254);
    public static readonly Color Success = Color.FromArgb(34, 168, 92);
    public static readonly Color Failure = Color.FromArgb(202, 62, 62);
    public static readonly Color Pending = Color.FromArgb(158, 172, 188);
    public static readonly Color ProgressTrack = Color.FromArgb(226, 234, 243);
    public static readonly Color ProgressFill = Color.FromArgb(43, 176, 76);
    public static readonly Color Glyph = Color.FromArgb(147, 163, 181);
    public static readonly Color Illustration = Color.FromArgb(216, 231, 252);
    public static readonly Color IllustrationDeep = Color.FromArgb(176, 206, 248);
    public static readonly Color FieldBorder = Color.FromArgb(216, 225, 236);
    public static readonly Color CheckboxBorder = Color.FromArgb(196, 209, 224);
    public static readonly Color HeaderLabel = Color.FromArgb(58, 78, 104);
    public static readonly Color LeadColor = Color.FromArgb(62, 95, 134);

    public static int CardRadius => S(8);

    public static int ControlRadius => S(6);

    public static int RowHeight => S(46);

    public static int ListHeaderHeight => S(37);

    public static readonly Font Title = new("Segoe UI", 22F, FontStyle.Bold);
    public static readonly Font Lead = new("Segoe UI", 11F);
    public static readonly Font Caption = new("Segoe UI", 9F);
    public static readonly Font Body = new("Segoe UI", 10F);
    public static readonly Font BodyStrong = new("Segoe UI Semibold", 10F);
    public static readonly Font ButtonText = new("Segoe UI Semibold", 10F);
    public static readonly Font ButtonTextLarge = new("Segoe UI Semibold", 11F);
    public static readonly Font Status = new("Segoe UI Semibold", 11F);
    public static readonly Font Badge = new("Segoe UI Semibold", 9F);
    public static readonly Font BadgeMedium = new("Segoe UI Semibold", 7.5F);
    public static readonly Font BadgeSmall = new("Segoe UI Semibold", 6F);

    public static GraphicsPath Rounded(RectangleF bounds, float radius)
    {
        var path = new GraphicsPath();
        if (radius <= 0f || bounds.Width <= 0f || bounds.Height <= 0f)
        {
            path.AddRectangle(bounds);
            return path;
        }

        var d = Math.Min(radius * 2f, Math.Min(bounds.Width, bounds.Height));
        path.AddArc(bounds.Left, bounds.Top, d, d, 180f, 90f);
        path.AddArc(bounds.Right - d, bounds.Top, d, d, 270f, 90f);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0f, 90f);
        path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90f, 90f);
        path.CloseFigure();
        return path;
    }

    /// <summary>Rectangle with only the bottom corners rounded.</summary>
    public static GraphicsPath RoundedBottom(RectangleF bounds, float radius)
    {
        var path = new GraphicsPath();
        if (radius <= 0f)
        {
            path.AddRectangle(bounds);
            return path;
        }

        var d = Math.Min(radius * 2f, Math.Min(bounds.Width, bounds.Height));
        path.AddLine(bounds.Left, bounds.Top, bounds.Right, bounds.Top);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0f, 90f);
        path.AddLine(bounds.Right, bounds.Bottom - d, bounds.Right - d, bounds.Bottom);
        path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90f, 90f);
        path.CloseFigure();
        return path;
    }
}
