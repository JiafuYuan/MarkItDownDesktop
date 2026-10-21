using System.Drawing.Drawing2D;

namespace MarkItDownDesktop.Ui;

/// <summary>Card holding the select all header row and the file list.</summary>
internal sealed class FileListCard : CardPanel
{
    private const int Inset = 1;

    private readonly FileListView list = new();

    public FileListCard()
    {
        FillColor = Color.White;
        SurroundColor = Theme.Page;
        Controls.Add(list);

        list.CheckedChanged += () =>
        {
            Invalidate();
            CheckedChanged?.Invoke();
        };
        list.MoreRequested += (item, point) => MoreRequested?.Invoke(item, point);
    }

    public FileListView FileList => list;

    public event Action? CheckedChanged;

    public event Action<FileItem, Point>? MoreRequested;

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        LayoutList();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        LayoutList();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (list.Interactive && IsSelectAllHit(e.Location))
        {
            list.ToggleAll();
            return;
        }

        base.OnMouseDown(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var header = new RectangleF(Inset, Inset, Width - Inset * 2f, Theme.ListHeaderHeight);
        var state = g.Save();
        using (var clip = CreateInnerPath())
        {
            g.SetClip(clip, CombineMode.Intersect);
            using var fill = new SolidBrush(Theme.SurfaceSubtle);
            g.FillRectangle(fill, header);
        }

        g.Restore(state);

        using (var pen = new Pen(Theme.Divider))
        {
            g.DrawLine(pen, Inset, Theme.ListHeaderHeight, Width - Inset, Theme.ListHeaderHeight);
        }

        FileListView.DrawCheckBox(
            g,
            FileListView.CheckBoxBounds(Inset, Theme.ListHeaderHeight - Inset),
            list.AllChecked,
            list.SomeChecked);

        string[] labels = ["文件", "类型", "状态", "输出"];
        for (var i = 0; i < labels.Length; i++)
        {
            var left = Inset + list.ColumnLeft(i + 1) + Theme.S(17);
            var width = list.Columns[i + 1].Width - Theme.S(22);
            if (width <= 0) continue;

            TextRenderer.DrawText(
                g,
                labels[i],
                Theme.BodyStrong,
                new Rectangle(left, Inset, width, Theme.ListHeaderHeight - Inset),
                i == 0 ? Theme.Text : Theme.HeaderLabel,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
        }

        for (var column = 2; column <= 4; column++)
        {
            var x = Inset + list.ColumnLeft(column);
            using var pen = new Pen(Theme.Divider);
            g.DrawLine(pen, x, Theme.ListHeaderHeight * 0.28f, x, Theme.ListHeaderHeight * 0.78f);
        }
    }

    private void LayoutList()
    {
        if (!IsHandleCreated || Width <= 0 || Height <= 0) return;

        var height = Math.Max(0, Height - Theme.ListHeaderHeight - Inset);
        list.Bounds = new Rectangle(Inset, Theme.ListHeaderHeight, Math.Max(0, Width - Inset * 2), height);
        list.LayoutColumns();

        using var path = Theme.RoundedBottom(new RectangleF(0f, 0f, list.Width, list.Height), Theme.CardRadius - 1);
        var previous = list.Region;
        list.Region = new Region(path);
        previous?.Dispose();
    }

    private bool IsSelectAllHit(Point point)
    {
        if (point.Y > Theme.ListHeaderHeight) return false;
        var box = FileListView.CheckBoxBounds(Inset, Theme.ListHeaderHeight - Inset);
        return point.X >= box.Left - 8 && point.X <= box.Right + 8;
    }
}
