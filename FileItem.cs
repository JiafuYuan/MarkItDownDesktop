namespace MarkItDownDesktop;

internal static class FileStatus
{
    public const string Pending = "等待转换";
    public const string Converting = "正在转换";
    public const string Done = "已完成";
    public const string Failed = "转换失败";
}

internal sealed class FileItem(string inputPath)
{
    public string InputPath { get; } = inputPath;

    public string FileName => Path.GetFileName(InputPath);

    public string Extension => Path.GetExtension(InputPath).TrimStart('.').ToUpperInvariant();

    public string Status { get; set; } = FileStatus.Pending;

    public string? OutputPath { get; set; }

    public string? Error { get; set; }

    public bool Checked { get; set; }
}
