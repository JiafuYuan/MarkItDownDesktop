using System.Drawing.Drawing2D;

namespace MarkItDownDesktop.Ui;

/// <summary>Owner drawn file list: checkbox column, file badge, status and per row menu.</summary>
internal sealed class FileListView : ListView
{
    public const int CheckBoxColumn = 0;
    public const int NameColumn = 1;
    public const int TypeColumn = 2;
    public const int StatusColumn = 3;
    public const int OutputColumn = 4;
    public const int MoreColumn = 5;

    /// <summary>Left edge of the checkbox inside a row, in device pixels.</summary>
    public static int CheckBoxLeft => Theme.S(27);

    public static int CheckBoxSize => Theme.S(15);

    private static int CellPadding => Theme.S(17);
    private static int BadgeSize => Theme.S(26);
    private static int BadgeLeft => Theme.S(6);
    private static int BadgeGap => Theme.S(14);

    private int hovered = -1;
    private bool interactive = true;

    public FileListView()
    {
        OwnerDraw = true;
        View = View.Details;
        HeaderStyle = ColumnHeaderStyle.None;
        FullRowSelect = true;
        MultiSelect = true;
        HideSelection = false;
        BorderStyle = BorderStyle.None;
        BackColor = Color.White;
        ForeColor = Theme.Text;
        Font = Theme.Body;
        AllowDrop = true;
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

        Columns.Add("选择", Theme.S(48));
        Columns.Add("文件", Theme.S(341));
        Columns.Add("类型", Theme.S(91));
        Columns.Add("状态", Theme.S(106));
        Columns.Add("输出", Theme.S(217));
        Columns.Add("操作", Theme.S(25));

        var rows = new ImageList { ImageSize = new Size(1, Theme.RowHeight), ColorDepth = ColorDepth.Depth32Bit };
        rows.Images.Add(new Bitmap(1, Theme.RowHeight));
        SmallImageList = rows;
    }

    public event Action? CheckedChanged;

    public event Action<FileItem, Point>? MoreRequested;

    /// <summary>
    /// When false the list keeps its normal appearance but ignores clicks, so the card does not
    /// turn into the gray disabled control background while a conversion is running.
    /// </summary>
    public bool Interactive
    {
        get => interactive;
        set
        {
            if (interactive == value) return;
            interactive = value;
            hovered = -1;
            Invalidate();
        }
    }

    public IEnumerable<FileItem> FileItems => Items.Cast<ListViewItem>().Select(row => row.Tag).OfType<FileItem>();

    public IEnumerable<FileItem> CheckedFiles => FileItems.Where(item => item.Checked);

    public int CheckedCount => CheckedFiles.Count();

    public bool AllChecked => Items.Count > 0 && FileItems.All(item => item.Checked);

    public bool SomeChecked => !AllChecked && CheckedCount > 0;

    public int ColumnLeft(int index)
    {
        var left = 0;
        for (var i = 0; i < index && i < Columns.Count; i++) left += Columns[i].Width;
        return left;
    }

    public void LayoutColumns()
    {
        if (Columns.Count < 6) return;

        var fixedWidth = Columns[CheckBoxColumn].Width + Columns[TypeColumn].Width + Columns[StatusColumn].Width + Columns[MoreColumn].Width;
        var remaining = Math.Max(Theme.S(240), ClientSize.Width - fixedWidth);
        var nameWidth = (int)Math.Round(remaining * 0.61);
        Columns[NameColumn].Width = nameWidth;
        Columns[OutputColumn].Width = remaining - nameWidth;
    }

    public void ToggleAll()
    {
        var check = !AllChecked;
        foreach (var item in FileItems) item.Checked = check;
        Invalidate();
        CheckedChanged?.Invoke();
    }

    public static Rectangle CheckBoxBounds(int rowTop, int rowHeight) =>
        new(CheckBoxLeft, rowTop + (rowHeight - CheckBoxSize) / 2, CheckBoxSize, CheckBoxSize);

    public static void DrawCheckBox(Graphics g, Rectangle box, bool isChecked, bool indeterminate)
    {
        var previous = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = Theme.Rounded(box, Theme.S(3f));
        if (isChecked || indeterminate)
        {
            using (var fill = new SolidBrush(Theme.Accent))
            {
                g.FillPath(fill, path);
            }

            if (indeterminate)
            {
                using var bar = new SolidBrush(Color.White);
                g.FillRectangle(bar, box.X + box.Width * 0.26f, box.Y + box.Height / 2f - Theme.S(1f), box.Width * 0.48f, Theme.S(2f));
            }
            else
            {
                using var pen = new Pen(Color.White, Theme.S(1.4f)) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };;
                g.DrawLines(pen,
                [
                    new PointF(box.X + box.Width * 0.24f, box.Y + box.Height * 0.52f),
                    new PointF(box.X + box.Width * 0.43f, box.Y + box.Height * 0.70f),
                    new PointF(box.X + box.Width * 0.76f, box.Y + box.Height * 0.31f),
                ]);
            }
        }
        else
        {
            using (var fill = new SolidBrush(Color.White))
            {
                g.FillPath(fill, path);
            }

            using var pen = new Pen(Theme.CheckboxBorder, Theme.S(1f));
            g.DrawPath(pen, path);
        }

        g.SmoothingMode = previous;
    }

    protected override void OnDrawItem(DrawListViewItemEventArgs e)
    {
        var g = e.Graphics;
        var row = new Rectangle(0, e.Bounds.Top, ClientSize.Width, e.Bounds.Height);
        var background = e.Item!.Selected ? Theme.RowSelected : e.ItemIndex == hovered ? Theme.RowHover : Color.White;
        using (var brush = new SolidBrush(background))
        {
            g.FillRectangle(brush, row);
        }

        using var pen = new Pen(Theme.Divider);
        g.DrawLine(pen, 0, row.Bottom - 1, row.Right, row.Bottom - 1);
    }

    protected override void OnDrawSubItem(DrawListViewSubItemEventArgs e)
    {
        if (e.Item?.Tag is not FileItem item) return;

        var g = e.Graphics;
        var left = ColumnLeft(e.ColumnIndex);
        var width = Columns[e.ColumnIndex].Width;
        var cell = new Rectangle(left, e.Bounds.Top, width, e.Bounds.Height);
        var state = g.Save();
        g.SetClip(cell);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        switch (e.ColumnIndex)
        {
            case CheckBoxColumn:
                DrawCheckBox(g, CheckBoxBounds(cell.Top, cell.Height), item.Checked, false);
                break;

            case NameColumn:
                DrawName(g, cell, item);
                break;

            case TypeColumn:
                DrawText(g, cell, item.Extension, Theme.Body, Theme.Text, CellPadding);
                break;

            case StatusColumn:
                DrawStatus(g, cell, item);
                break;

            case OutputColumn:
                DrawText(g, cell, item.OutputPath is null ? string.Empty : Path.GetFileName(item.OutputPath), Theme.Body, Theme.Text, CellPadding);
                break;

            case MoreColumn:
                var moreSize = Theme.S(14);
                Icons.Draw(g, Glyph.More, new RectangleF(cell.X + cell.Width / 2f - moreSize / 2f, cell.Y + cell.Height / 2f - moreSize / 2f, moreSize, moreSize), Theme.Glyph);
                break;
        }

        g.Restore(state);
    }

    private static void DrawName(Graphics g, Rectangle cell, FileItem item)
    {
        var (label, color) = FileTypeBadges.For(item.InputPath);
        var size = BadgeSize;
        var badge = new RectangleF(cell.X + BadgeLeft, cell.Y + (cell.Height - size) / 2f, size, size);
        Icons.FileBadge(g, badge, label, color);

        var textLeft = (int)badge.Right + BadgeGap;
        DrawText(g, cell, item.FileName, Theme.Body, Theme.Text, textLeft - cell.X);
    }

    private static void DrawStatus(Graphics g, Rectangle cell, FileItem item)
    {
        var iconSize = Theme.S(14);
        var iconX = cell.X + Theme.S(3);
        var box = new RectangleF(iconX, cell.Y + (cell.Height - iconSize) / 2f, iconSize, iconSize);
        Color color;
        switch (item.Status)
        {
            case FileStatus.Done:
                Icons.CheckCircle(g, box, Theme.Success);
                color = Theme.Success;
                break;
            case FileStatus.Failed:
                Icons.AlertCircle(g, box, Theme.Failure);
                color = Theme.Failure;
                break;
            case FileStatus.Converting:
                Icons.EmptyCircle(g, box, Theme.Accent);
                color = Theme.Accent;
                break;
            default:
                Icons.EmptyCircle(g, box, Theme.Pending);
                color = Theme.Pending;
                break;
        }

        var padding = (int)box.Right - cell.X + Theme.S(6);
        DrawText(g, cell, item.Status, Theme.BodyStrong, color, padding);
    }

    private static void DrawText(Graphics g, Rectangle cell, string text, Font font, Color color, int padding)
    {
        if (string.IsNullOrEmpty(text)) return;

        var bounds = new Rectangle(cell.X + padding, cell.Y, Math.Max(1, cell.Width - padding - Theme.S(6)), cell.Height);
        TextRenderer.DrawText(
            g,
            text,
            font,
            bounds,
            color,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!interactive) return;
        var index = HitTest(e.Location).Item?.Index ?? -1;
        if (index == hovered) return;
        hovered = index;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (hovered < 0) return;
        hovered = -1;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (!interactive)
        {
            base.OnMouseDown(e);
            return;
        }

        var info = HitTest(e.Location);
        if (info.Item?.Tag is FileItem item)
        {
            switch (ColumnAt(e.X))
            {
                case CheckBoxColumn:
                    item.Checked = !item.Checked;
                    Invalidate();
                    CheckedChanged?.Invoke();
                    return;
                case MoreColumn:
                    MoreRequested?.Invoke(item, new Point(e.X, info.Item.Bounds.Bottom));
                    return;
            }
        }

        base.OnMouseDown(e);
    }

    private int ColumnAt(int x)
    {
        for (var i = Columns.Count - 1; i >= 0; i--)
        {
            if (x >= ColumnLeft(i)) return i;
        }

        return -1;
    }
}
