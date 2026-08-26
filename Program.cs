using System.Diagnostics;

namespace MarkItDownDesktop;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm(args));
    }
}

internal sealed class MainForm : Form
{
    private readonly ListView filesView = new();
    private readonly TextBox outputDirectoryBox = new();
    private readonly Button convertButton = new();
    private readonly Button removeButton = new();
    private readonly Label statusLabel = new();
    private readonly ProgressBar progressBar = new();
    private readonly List<FileItem> files = [];
    private readonly string enginePath = Path.Combine(AppContext.BaseDirectory, "markitdown.exe");

    public MainForm(IEnumerable<string>? startupFiles = null)
    {
        Text = "MarkItDown";
        MinimumSize = new Size(900, 650);
        Size = new Size(1080, 760);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(246, 247, 249);
        Font = new Font("Segoe UI", 10F);
        AllowDrop = true;

        BuildLayout();
        outputDirectoryBox.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        DragEnter += OnDragEnter;
        DragDrop += OnDragDrop;
        if (startupFiles is not null) AddFiles(startupFiles);
    }

    private void BuildLayout()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(28),
            ColumnCount = 1,
            RowCount = 6,
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(layout);

        var header = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(246, 247, 249) };
        header.Controls.Add(new Label
        {
            Text = "MarkItDown",
            Font = new Font("Segoe UI Semibold", 30F),
            ForeColor = Color.FromArgb(20, 51, 82),
            AutoSize = true,
            Location = new Point(0, 0),
        });
        header.Controls.Add(new Label
        {
            Text = "将文档转换为 Markdown",
            ForeColor = Color.FromArgb(92, 101, 115),
            AutoSize = true,
            Font = new Font("Segoe UI", 11F),
            Location = new Point(4, 54),
        });
        layout.Controls.Add(header, 0, 0);

        var dropZone = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(235, 244, 251),
            BorderStyle = BorderStyle.FixedSingle,
            Cursor = Cursors.Hand,
            AllowDrop = true,
        };
        var dropTitle = new Label
        {
            Text = "拖放文件到这里",
            Font = new Font("Segoe UI Semibold", 13F),
            ForeColor = Color.FromArgb(20, 74, 116),
            AutoSize = true,
            Location = new Point(20, 13),
        };
        var dropSubtitle = new Label
        {
            Text = "支持批量添加，或点击此区域选择文件",
            Font = new Font("Segoe UI", 10F),
            ForeColor = Color.FromArgb(83, 105, 122),
            AutoSize = true,
            Location = new Point(21, 43),
        };
        dropZone.Controls.Add(dropTitle);
        dropZone.Controls.Add(dropSubtitle);
        dropZone.Click += (_, _) => AddFilesFromDialog();
        dropTitle.Click += (_, _) => AddFilesFromDialog();
        dropSubtitle.Click += (_, _) => AddFilesFromDialog();
        dropZone.DragEnter += OnDragEnter;
        dropZone.DragDrop += OnDragDrop;
        layout.Controls.Add(dropZone, 0, 1);

        var filePanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 14, 0, 14) };
        filesView.Dock = DockStyle.Fill;
        filesView.BackColor = Color.White;
        filesView.BorderStyle = BorderStyle.FixedSingle;
        filesView.Font = new Font("Segoe UI", 10F);
        filesView.View = View.Details;
        filesView.FullRowSelect = true;
        filesView.GridLines = false;
        filesView.HideSelection = false;
        filesView.AllowDrop = true;
        filesView.Columns.Add("文件", 430);
        filesView.Columns.Add("类型", 100);
        filesView.Columns.Add("状态", 150);
        filesView.Columns.Add("输出", 240);
        filesView.DragEnter += OnDragEnter;
        filesView.DragDrop += OnDragDrop;
        filesView.SelectedIndexChanged += (_, _) => removeButton.Enabled = filesView.SelectedItems.Count > 0;

        filePanel.Controls.Add(filesView);
        layout.Controls.Add(filePanel, 0, 2);

        var fileActions = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        var addButton = CreateButton("添加文件", AddFilesFromDialog);
        removeButton.Text = "移除所选";
        removeButton.AutoSize = true;
        removeButton.Enabled = false;
        removeButton.Click += (_, _) => RemoveSelectedFiles();
        fileActions.Controls.Add(addButton);
        fileActions.Controls.Add(removeButton);
        layout.Controls.Add(fileActions, 0, 3);

        var outputPanel = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 3, Padding = new Padding(0, 16, 0, 0) };
        outputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        outputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        outputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        outputPanel.Controls.Add(new Label { Text = "保存到", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 0, 12, 0) }, 0, 0);
        outputDirectoryBox.Dock = DockStyle.Fill;
        outputDirectoryBox.ReadOnly = true;
        outputPanel.Controls.Add(outputDirectoryBox, 1, 0);
        outputPanel.Controls.Add(CreateButton("选择文件夹", ChooseOutputDirectory), 2, 0);
        layout.Controls.Add(outputPanel, 0, 4);

        var footer = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(0, 18, 0, 0) };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var progressPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        statusLabel.Text = "添加一个或多个文件以开始";
        statusLabel.ForeColor = Color.FromArgb(84, 94, 108);
        statusLabel.AutoSize = true;
        progressBar.Dock = DockStyle.Top;
        progressBar.Height = 6;
        progressPanel.Controls.Add(statusLabel, 0, 0);
        progressPanel.Controls.Add(progressBar, 0, 1);
        convertButton.Text = "开始转换";
        convertButton.AutoSize = true;
        convertButton.Padding = new Padding(16, 7, 16, 7);
        convertButton.BackColor = Color.FromArgb(20, 94, 179);
        convertButton.ForeColor = Color.White;
        convertButton.FlatStyle = FlatStyle.Flat;
        convertButton.FlatAppearance.BorderSize = 0;
        convertButton.Click += async (_, _) => await ConvertFilesAsync();
        footer.Controls.Add(progressPanel, 0, 0);
        footer.Controls.Add(convertButton, 1, 0);
        layout.Controls.Add(footer, 0, 5);
    }

    private Button CreateButton(string text, Action action)
    {
        var button = new Button { Text = text, AutoSize = true, Padding = new Padding(8, 4, 8, 4), Margin = new Padding(0, 0, 8, 0) };
        button.Click += (_, _) => action();
        return button;
    }

    private void AddFilesFromDialog()
    {
        using var dialog = new OpenFileDialog { Multiselect = true, Title = "选择要转换的文件", Filter = "所有文件|*.*" };
        if (dialog.ShowDialog(this) == DialogResult.OK) AddFiles(dialog.FileNames);
    }

    private void ChooseOutputDirectory()
    {
        using var dialog = new FolderBrowserDialog { Description = "选择 Markdown 文件保存位置" };
        if (dialog.ShowDialog(this) == DialogResult.OK) outputDirectoryBox.Text = dialog.SelectedPath;
    }

    private void OnDragEnter(object? sender, DragEventArgs e) => e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true ? DragDropEffects.Copy : DragDropEffects.None;

    private void OnDragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] paths) AddFiles(paths);
    }

    private void AddFiles(IEnumerable<string> paths)
    {
        foreach (var path in paths.Where(File.Exists))
        {
            if (files.Any(item => string.Equals(item.InputPath, path, StringComparison.OrdinalIgnoreCase))) continue;
            var item = new FileItem(path);
            files.Add(item);
            filesView.Items.Add(CreateListItem(item));
        }
        statusLabel.Text = files.Count == 0 ? "添加一个或多个文件以开始" : $"已添加 {files.Count} 个文件";
    }

    private ListViewItem CreateListItem(FileItem item) => new([Path.GetFileName(item.InputPath), Path.GetExtension(item.InputPath).TrimStart('.').ToUpperInvariant(), item.Status, item.OutputPath is null ? "" : Path.GetFileName(item.OutputPath)]) { Tag = item };

    private void RemoveSelectedFiles()
    {
        foreach (ListViewItem row in filesView.SelectedItems)
        {
            if (row.Tag is FileItem item) files.Remove(item);
            filesView.Items.Remove(row);
        }
        statusLabel.Text = files.Count == 0 ? "添加一个或多个文件以开始" : $"已添加 {files.Count} 个文件";
    }

    private async Task ConvertFilesAsync()
    {
        if (files.Count == 0) { statusLabel.Text = "请先添加文件"; return; }
        if (!File.Exists(enginePath)) { MessageBox.Show(this, "未找到 markitdown.exe。请确保它与本程序放在同一个文件夹。", "转换引擎缺失", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        Directory.CreateDirectory(outputDirectoryBox.Text);
        SetControlsEnabled(false);
        progressBar.Minimum = 0;
        progressBar.Maximum = files.Count;
        progressBar.Value = 0;
        var failed = 0;

        for (var index = 0; index < files.Count; index++)
        {
            var item = files[index];
            item.Status = "正在转换";
            UpdateRow(item);
            statusLabel.Text = $"正在转换 {index + 1}/{files.Count}: {Path.GetFileName(item.InputPath)}";
            item.OutputPath = GetOutputPath(item.InputPath);
            var result = await RunEngineAsync(item.InputPath, item.OutputPath);
            item.Status = result.Success ? "已完成" : "转换失败";
            if (!result.Success) { item.Error = result.Error; failed++; }
            UpdateRow(item);
            progressBar.Value = index + 1;
        }

        SetControlsEnabled(true);
        statusLabel.Text = failed == 0 ? $"已完成 {files.Count} 个文件" : $"完成，{failed} 个文件转换失败";
        if (failed > 0) MessageBox.Show(this, "部分文件未能转换。选择对应文件可在状态栏中查看结果。", "转换完成", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private async Task<ConversionResult> RunEngineAsync(string input, string output)
    {
        using var process = new Process { StartInfo = new ProcessStartInfo(enginePath) { UseShellExecute = false, RedirectStandardError = true, RedirectStandardOutput = true, CreateNoWindow = true } };
        process.StartInfo.ArgumentList.Add(input);
        process.StartInfo.ArgumentList.Add("--output");
        process.StartInfo.ArgumentList.Add(output);
        process.Start();
        var errors = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return process.ExitCode == 0 ? new ConversionResult(true, "") : new ConversionResult(false, string.IsNullOrWhiteSpace(errors) ? "转换器未返回错误详情" : errors.Trim());
    }

    private string GetOutputPath(string input)
    {
        var name = Path.GetFileNameWithoutExtension(input);
        if (string.Equals(Path.GetExtension(input), ".md", StringComparison.OrdinalIgnoreCase)) name += "_converted";
        var candidate = Path.Combine(outputDirectoryBox.Text, name + ".md");
        for (var suffix = 2; File.Exists(candidate); suffix++) candidate = Path.Combine(outputDirectoryBox.Text, $"{name}_{suffix}.md");
        return candidate;
    }

    private void UpdateRow(FileItem item)
    {
        var row = filesView.Items.Cast<ListViewItem>().First(row => ReferenceEquals(row.Tag, item));
        row.SubItems[2].Text = item.Status;
        row.SubItems[3].Text = item.OutputPath is null ? "" : Path.GetFileName(item.OutputPath);
        row.ForeColor = item.Status == "转换失败" ? Color.FromArgb(180, 38, 38) : Color.FromArgb(31, 41, 55);
    }

    private void SetControlsEnabled(bool enabled)
    {
        convertButton.Enabled = enabled;
        removeButton.Enabled = enabled && filesView.SelectedItems.Count > 0;
        filesView.Enabled = enabled;
    }

    private sealed class FileItem(string inputPath)
    {
        public string InputPath { get; } = inputPath;
        public string Status { get; set; } = "等待转换";
        public string? OutputPath { get; set; }
        public string? Error { get; set; }
    }

    private readonly record struct ConversionResult(bool Success, string Error);
}
