using System.Diagnostics;
using System.Runtime.InteropServices;
using MarkItDownDesktop.Ui;

namespace MarkItDownDesktop;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        Theme.Scale = SystemDpi() / 96f;
        Application.Run(new MainForm(args));
    }

    /// <summary>System DPI, used to scale the hand written layout on high DPI displays.</summary>
    private static int SystemDpi()
    {
        try
        {
            var dpi = GetDpiForSystem();
            return dpi >= 48 && dpi <= 480 ? (int)dpi : 96;
        }
        catch (EntryPointNotFoundException)
        {
            return 96;
        }
    }

    [DllImport("user32.dll")]
    private static extern uint GetDpiForSystem();
}

internal sealed class MainForm : Form
{
    private readonly HeaderCard headerCard = new();
    private readonly FileListCard fileCard = new();
    private readonly FieldBox outputBox = new();
    private readonly AccentButton addButton = new();
    private readonly AccentButton removeButton = new();
    private readonly AccentButton chooseFolderButton = new();
    private readonly AccentButton convertButton = new();
    private readonly StatusLine statusLine = new();
    private readonly RoundedProgressBar progressBar = new();
    private readonly List<FileItem> files = [];
    private readonly string enginePath = Path.Combine(AppContext.BaseDirectory, "markitdown.exe");
    private string outputDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    private bool isConverting;
    private bool isUpdating;

    public MainForm(IEnumerable<string>? startupFiles = null)
    {
        Text = "MarkItDown";
        MinimumSize = Theme.S(760, 560);
        Size = Theme.S(884, 616);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Theme.Page;
        Font = Theme.Body;
        AllowDrop = true;
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;

        BuildLayout();

        outputBox.Value = outputDirectory;
        DragEnter += OnDragEnter;
        DragDrop += OnDragDrop;
        if (startupFiles is not null) AddFiles(startupFiles);
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        UpdateEngineButtonText();
        if (File.Exists(enginePath)) return;
        await EnsureEngineAsync();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyCaptionColors();
    }

    private void BuildLayout()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(Theme.S(12), Theme.S(10), Theme.S(12), Theme.S(12)),
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Theme.Page,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.S(135)));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.S(116)));
        Controls.Add(layout);

        headerCard.Dock = DockStyle.Fill;
        headerCard.Margin = new Padding(0, 0, 0, Theme.S(8));
        headerCard.CardClicked += (_, _) => AddFilesFromDialog();
        headerCard.DragEnter += OnDragEnter;
        headerCard.DragDrop += OnDragDrop;
        headerCard.UpdateButton.Click += async (_, _) => await CheckForEngineUpdateAsync();
        layout.Controls.Add(headerCard, 0, 0);

        fileCard.Dock = DockStyle.Fill;
        fileCard.Margin = new Padding(0, 0, 0, Theme.S(16));
        fileCard.CheckedChanged += UpdateActionButtons;
        fileCard.MoreRequested += ShowRowMenu;
        fileCard.FileList.DragEnter += OnDragEnter;
        fileCard.FileList.DragDrop += OnDragDrop;
        fileCard.FileList.SelectedIndexChanged += (_, _) => UpdateActionButtons();
        layout.Controls.Add(fileCard, 0, 1);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0, 0, 0, Theme.S(10)),
            BackColor = Theme.Page,
        };
        addButton.Text = "添加文件";
        addButton.Icon = Glyph.Plus;
        addButton.MinimumHeight = Theme.S(36);
        addButton.Margin = new Padding(0, 0, Theme.S(10), 0);
        addButton.Click += (_, _) => AddFilesFromDialog();
        removeButton.Text = "移除所选";
        removeButton.Icon = Glyph.Trash;
        removeButton.Primary = false;
        removeButton.MinimumHeight = Theme.S(36);
        removeButton.Enabled = false;
        removeButton.Margin = new Padding(0);
        removeButton.Click += (_, _) => RemoveSelectedFiles();
        actions.Controls.Add(addButton);
        actions.Controls.Add(removeButton);
        layout.Controls.Add(actions, 0, 2);

        layout.Controls.Add(BuildBottomCard(), 0, 3);
    }

    private CardPanel BuildBottomCard()
    {
        var card = new CardPanel
        {
            Dock = DockStyle.Fill,
            FillColor = Theme.SurfaceSubtle,
            SurroundColor = Theme.Page,
            Padding = new Padding(Theme.S(14), Theme.S(12), Theme.S(14), Theme.S(10)),
            Margin = new Padding(0),
        };

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 2,
            BackColor = Theme.SurfaceSubtle,
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.S(38)));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var label = new Label
        {
            Text = "保存到",
            AutoSize = true,
            Font = Theme.BodyStrong,
            ForeColor = Theme.Text,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(Theme.S(2), 0, Theme.S(10), 0),
        };
        grid.Controls.Add(label, 0, 0);

        outputBox.Dock = DockStyle.Fill;
        outputBox.Margin = new Padding(0, Theme.S(1), Theme.S(12), Theme.S(1));
        outputBox.FieldClicked += (_, _) => ChooseOutputDirectory();
        grid.Controls.Add(outputBox, 1, 0);

        chooseFolderButton.Text = "选择文件夹";
        chooseFolderButton.Primary = false;
        chooseFolderButton.HorizontalPadding = Theme.S(15);
        chooseFolderButton.MinimumHeight = Theme.S(36);
        chooseFolderButton.Anchor = AnchorStyles.Right;
        chooseFolderButton.Margin = new Padding(0, Theme.S(1), 0, Theme.S(1));
        chooseFolderButton.Click += (_, _) => ChooseOutputDirectory();
        grid.Controls.Add(chooseFolderButton, 2, 0);

        var statusPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Theme.SurfaceSubtle,
            Margin = new Padding(Theme.S(2), Theme.S(9), Theme.S(12), 0),
        };
        statusPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.S(18)));
        statusPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.S(14)));
        statusLine.Dock = DockStyle.Fill;
        statusLine.Margin = new Padding(0);
        statusLine.Text = "添加一个或多个文件以开始";
        progressBar.Dock = DockStyle.Fill;
        progressBar.Margin = new Padding(0, Theme.S(6), 0, 0);
        progressBar.SurroundColor = Theme.SurfaceSubtle;
        progressBar.Maximum = 100;
        statusPanel.Controls.Add(statusLine, 0, 0);
        statusPanel.Controls.Add(progressBar, 0, 1);
        grid.Controls.Add(statusPanel, 0, 1);
        grid.SetColumnSpan(statusPanel, 2);

        convertButton.Text = "开始转换";
        convertButton.Icon = Glyph.Refresh;
        convertButton.MinimumHeight = Theme.S(46);
        convertButton.HorizontalPadding = Theme.S(18);
        convertButton.IconSize = Theme.S(15);
        convertButton.Font = Theme.ButtonTextLarge;
        convertButton.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
        convertButton.Margin = new Padding(0);
        convertButton.Click += async (_, _) => await ConvertFilesAsync();
        grid.Controls.Add(convertButton, 2, 1);

        card.Controls.Add(grid);
        return card;
    }

    private void AddFilesFromDialog()
    {
        using var dialog = new OpenFileDialog { Multiselect = true, Title = "选择要转换的文件", Filter = "所有文件|*.*" };
        if (dialog.ShowDialog(this) == DialogResult.OK) AddFiles(dialog.FileNames);
    }

    private void ChooseOutputDirectory()
    {
        using var dialog = new FolderBrowserDialog { Description = "选择 Markdown 文件保存位置", SelectedPath = outputDirectory };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        outputDirectory = dialog.SelectedPath;
        outputBox.Value = outputDirectory;
    }

    private void OnDragEnter(object? sender, DragEventArgs e) =>
        e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true ? DragDropEffects.Copy : DragDropEffects.None;

    private void OnDragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] paths) AddFiles(paths);
    }

    private void AddFiles(IEnumerable<string> paths)
    {
        var added = false;
        foreach (var path in paths.Where(File.Exists))
        {
            if (files.Any(item => string.Equals(item.InputPath, path, StringComparison.OrdinalIgnoreCase))) continue;
            var item = new FileItem(path);
            files.Add(item);
            fileCard.FileList.Items.Add(CreateRow(item));
            added = true;
        }

        if (added) UpdateIdleStatus();
        UpdateActionButtons();
    }

    private static ListViewItem CreateRow(FileItem item) =>
        new([string.Empty, item.FileName, item.Extension, item.Status, string.Empty, string.Empty]) { Tag = item };

    private void RemoveSelectedFiles()
    {
        var targets = fileCard.FileList.CheckedFiles.ToList();
        if (targets.Count == 0)
        {
            targets = fileCard.FileList.SelectedItems.Cast<ListViewItem>()
                .Select(row => row.Tag)
                .OfType<FileItem>()
                .ToList();
        }

        if (targets.Count == 0) return;
        RemoveFiles(targets);
    }

    private void RemoveFiles(IEnumerable<FileItem> targets)
    {
        foreach (var item in targets.ToList())
        {
            files.Remove(item);
            var row = fileCard.FileList.Items.Cast<ListViewItem>().FirstOrDefault(candidate => ReferenceEquals(candidate.Tag, item));
            if (row is not null) fileCard.FileList.Items.Remove(row);
        }

        UpdateIdleStatus();
        UpdateActionButtons();
    }

    private void ShowRowMenu(FileItem item, Point location)
    {
        var menu = new ContextMenuStrip { ShowImageMargin = false, Font = Theme.Body };
        menu.Items.Add("打开输出位置", null, (_, _) => OpenOutputFolder(item));
        var retry = menu.Items.Add("重新转换", null, async (_, _) => await ConvertItemsAsync([item]));
        retry.Enabled = !isConverting && !isUpdating && File.Exists(enginePath);
        var details = menu.Items.Add("错误详情", null, (_, _) => ShowItemError(item));
        details.Enabled = !string.IsNullOrWhiteSpace(item.Error);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("移除", null, (_, _) => RemoveFiles([item]));

        // open towards the inside of the window so the menu stays visible
        var size = menu.GetPreferredSize(Size.Empty);
        var x = Math.Max(0, Math.Min(location.X, fileCard.FileList.ClientSize.Width - size.Width));
        menu.Show(fileCard.FileList, new Point(x, location.Y));
    }

    private void ShowItemError(FileItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Error)) return;
        MessageBox.Show(this, item.Error, $"{item.FileName} 转换失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private void OpenOutputFolder(FileItem item)
    {
        if (item.OutputPath is not null && File.Exists(item.OutputPath))
        {
            Process.Start("explorer.exe", $"/select,\"{item.OutputPath}\"");
            return;
        }

        if (Directory.Exists(outputDirectory))
        {
            Process.Start("explorer.exe", $"\"{outputDirectory}\"");
            return;
        }

        statusLine.Text = "输出文件夹尚未创建";
    }

    private async Task ConvertFilesAsync()
    {
        await ConvertItemsAsync(files.ToList());
    }

    private async Task ConvertItemsAsync(IReadOnlyList<FileItem> targets)
    {
        if (isConverting) return;
        if (isUpdating)
        {
            statusLine.Text = "正在下载转换引擎，请稍候";
            return;
        }

        if (targets.Count == 0)
        {
            statusLine.Text = "请先添加文件";
            return;
        }

        if (!File.Exists(enginePath))
        {
            MessageBox.Show(this, "未找到 markitdown.exe。请确保它与本程序放在同一个文件夹。", "转换引擎缺失", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        Directory.CreateDirectory(outputDirectory);
        isConverting = true;
        SetControlsEnabled(false);
        progressBar.Minimum = 0;
        progressBar.Maximum = targets.Count;
        progressBar.Value = 0;
        statusLine.Current = StatusLine.State.Idle;
        var failed = 0;

        try
        {
            for (var index = 0; index < targets.Count; index++)
            {
                var item = targets[index];
                item.Status = FileStatus.Converting;
                item.Error = null;
                fileCard.FileList.Invalidate();
                statusLine.Text = $"正在转换 {index + 1}/{targets.Count}: {item.FileName}";
                item.OutputPath = GetOutputPath(item.InputPath);
                try
                {
                    var result = await RunEngineAsync(item.InputPath, item.OutputPath);
                    item.Status = result.Success ? FileStatus.Done : FileStatus.Failed;
                    if (!result.Success)
                    {
                        item.Error = result.Error;
                        failed++;
                    }
                }
                catch (Exception exception)
                {
                    item.OutputPath = null;
                    item.Status = FileStatus.Failed;
                    item.Error = exception.Message;
                    failed++;
                }

                fileCard.FileList.Invalidate();
                progressBar.Value = index + 1;
            }
        }
        finally
        {
            isConverting = false;
            SetControlsEnabled(true);
        }

        UpdateIdleStatus();
        if (failed > 0)
        {
            statusLine.Current = StatusLine.State.Failure;
            progressBar.FillColor = Theme.Failure;
            MessageBox.Show(this, "部分文件未能转换。可点击每行的省略号菜单查看或重新转换。", "转换完成", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        else
        {
            statusLine.Current = StatusLine.State.Success;
        }
    }

    private async Task<ConversionResult> RunEngineAsync(string input, string output)
    {
        using var process = new Process { StartInfo = new ProcessStartInfo(enginePath) { UseShellExecute = false, RedirectStandardError = true, RedirectStandardOutput = true, CreateNoWindow = true } };
        process.StartInfo.ArgumentList.Add(input);
        process.StartInfo.ArgumentList.Add("--output");
        process.StartInfo.ArgumentList.Add(output);
        process.Start();
        var errorsTask = process.StandardError.ReadToEndAsync();
        var outputTask = process.StandardOutput.ReadToEndAsync();

        using var timeout = new CancellationTokenSource(EngineTimeout);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
                // the engine already exited between the timeout and the kill
            }

            await process.WaitForExitAsync();
            return new ConversionResult(false, $"转换超时（超过 {EngineTimeout.TotalMinutes:0} 分钟），已终止转换引擎。");
        }

        var errors = await errorsTask;
        await outputTask;
        return process.ExitCode == 0 ? new ConversionResult(true, string.Empty) : new ConversionResult(false, string.IsNullOrWhiteSpace(errors) ? "转换器未返回错误详情" : errors.Trim());
    }

    private static readonly TimeSpan EngineTimeout = TimeSpan.FromMinutes(10);

    private void UpdateEngineButtonText() => headerCard.UpdateButton.Text = File.Exists(enginePath) ? "检查引擎更新" : "下载转换引擎";

    private async Task EnsureEngineAsync()
    {
        isUpdating = true;
        SetControlsEnabled(false);
        statusLine.Text = "未找到转换引擎，正在自动下载...";
        try
        {
            var updater = new EngineUpdateService(enginePath);
            var release = await updater.GetLatestReleaseAsync(CancellationToken.None);
            await DownloadEngineAsync(updater, release, "正在下载转换引擎");
            statusLine.Text = $"转换引擎已就绪 {release.Version}";
            statusLine.Current = StatusLine.State.Success;
        }
        catch (Exception exception)
        {
            statusLine.Text = "转换引擎尚未安装";
            statusLine.Current = StatusLine.State.Failure;
            MessageBox.Show(this, $"无法自动下载转换引擎。\n\n{exception.Message}\n\n请点击“下载转换引擎”重试。", "转换引擎缺失", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            isUpdating = false;
            SetControlsEnabled(true);
            UpdateEngineButtonText();
        }
    }

    private async Task DownloadEngineAsync(EngineUpdateService updater, EngineRelease release, string action)
    {
        var progress = new Progress<int>(value =>
        {
            progressBar.Minimum = 0;
            progressBar.Maximum = 100;
            progressBar.Value = value;
            statusLine.Current = StatusLine.State.Idle;
            statusLine.Text = $"{action}: {value}%";
        });
        await updater.InstallAsync(release, progress, CancellationToken.None);
        progressBar.Value = 100;
    }

    private async Task CheckForEngineUpdateAsync()
    {
        if (isConverting || isUpdating)
        {
            MessageBox.Show(this, "请等待当前任务完成后再更新引擎。", "正在处理", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        headerCard.UpdateButton.Enabled = false;
        try
        {
            statusLine.Text = "正在检查引擎更新...";
            var updater = new EngineUpdateService(enginePath);
            var release = await updater.GetLatestReleaseAsync(CancellationToken.None);
            var installedVersion = await updater.GetInstalledVersionAsync(CancellationToken.None);
            if (!release.IsNewerThan(installedVersion))
            {
                statusLine.Text = $"转换引擎已是最新版本 {installedVersion ?? release.Version}";
                statusLine.Current = StatusLine.State.Success;
                MessageBox.Show(this, statusLine.Text, "检查更新", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var currentText = installedVersion ?? "未安装";
            var choice = MessageBox.Show(this, $"发现新的转换引擎。\n\n当前版本: {currentText}\n可用版本: {release.Version}\n\n现在下载并安装吗？", "引擎更新", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (choice != DialogResult.Yes)
            {
                statusLine.Text = "已取消引擎更新";
                return;
            }

            await DownloadEngineAsync(updater, release, "正在下载引擎更新");
            statusLine.Text = $"已更新转换引擎至 {release.Version}";
            statusLine.Current = StatusLine.State.Success;
            MessageBox.Show(this, statusLine.Text, "更新完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (EngineUpdateException exception)
        {
            statusLine.Text = "引擎更新不可用";
            statusLine.Current = StatusLine.State.Failure;
            MessageBox.Show(this, exception.Message, "引擎更新", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception exception)
        {
            statusLine.Text = "引擎更新失败";
            statusLine.Current = StatusLine.State.Failure;
            MessageBox.Show(this, $"无法完成引擎更新。\n\n{exception.Message}", "引擎更新", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            headerCard.UpdateButton.Enabled = true;
            UpdateEngineButtonText();
        }
    }

    private string GetOutputPath(string input)
    {
        var name = Path.GetFileNameWithoutExtension(input);
        if (string.Equals(Path.GetExtension(input), ".md", StringComparison.OrdinalIgnoreCase)) name += "_converted";
        var candidate = Path.Combine(outputDirectory, name + ".md");
        for (var suffix = 2; File.Exists(candidate); suffix++) candidate = Path.Combine(outputDirectory, $"{name}_{suffix}.md");
        return candidate;
    }

    private void UpdateIdleStatus()
    {
        statusLine.Current = StatusLine.State.Idle;
        progressBar.FillColor = Theme.ProgressFill;
        if (files.Count == 0)
        {
            statusLine.Text = "添加一个或多个文件以开始";
            progressBar.Value = 0;
            return;
        }

        var done = files.Count(item => item.Status == FileStatus.Done);
        var failed = files.Count(item => item.Status == FileStatus.Failed);
        if (done + failed == files.Count && failed == 0)
        {
            statusLine.Current = StatusLine.State.Success;
            statusLine.Text = $"已完成 {done} 个文件";
            progressBar.Value = progressBar.Maximum;
            return;
        }

        if (failed > 0)
        {
            statusLine.Current = StatusLine.State.Failure;
            statusLine.Text = $"完成，{failed} 个文件转换失败";
            progressBar.Value = progressBar.Maximum;
            return;
        }

        statusLine.Text = $"已添加 {files.Count} 个文件";
        progressBar.Value = 0;
    }

    private void UpdateActionButtons()
    {
        var hasSelection = fileCard.FileList.CheckedCount > 0 || fileCard.FileList.SelectedItems.Count > 0;
        removeButton.Enabled = !isConverting && !isUpdating && hasSelection;
        convertButton.Enabled = !isConverting && !isUpdating;
        addButton.Enabled = !isConverting && !isUpdating;
    }

    private void SetControlsEnabled(bool enabled)
    {
        addButton.Enabled = enabled;
        convertButton.Enabled = enabled;
        fileCard.FileList.Enabled = enabled;
        headerCard.UpdateButton.Enabled = enabled;
        UpdateActionButtons();
    }

    private readonly record struct ConversionResult(bool Success, string Error);

    private const int DwmwaBorderColor = 34;
    private const int DwmwaCaptionColor = 35;
    private const int DwmwaTextColor = 36;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    /// <summary>Tints the system title bar with the accent color on Windows 11.</summary>
    private void ApplyCaptionColors()
    {
        var caption = ColorTranslator.ToWin32(Theme.Accent);
        var border = ColorTranslator.ToWin32(Theme.Accent);
        var text = ColorTranslator.ToWin32(Color.White);
        DwmSetWindowAttribute(Handle, DwmwaCaptionColor, ref caption, sizeof(int));
        DwmSetWindowAttribute(Handle, DwmwaBorderColor, ref border, sizeof(int));
        DwmSetWindowAttribute(Handle, DwmwaTextColor, ref text, sizeof(int));
    }
}
