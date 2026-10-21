namespace MarkItDownDesktop.Ui;

/// <summary>Short label and color used by the file type badge in the list.</summary>
internal static class FileTypeBadges
{
    private static readonly Color Word = Color.FromArgb(43, 87, 154);
    private static readonly Color Excel = Color.FromArgb(33, 115, 70);
    private static readonly Color PowerPoint = Color.FromArgb(210, 71, 38);
    private static readonly Color Portable = Color.FromArgb(217, 48, 37);
    private static readonly Color Markdown = Color.FromArgb(46, 125, 247);
    private static readonly Color Markup = Color.FromArgb(228, 77, 38);
    private static readonly Color Data = Color.FromArgb(222, 145, 17);
    private static readonly Color Plain = Color.FromArgb(96, 125, 139);
    private static readonly Color Image = Color.FromArgb(124, 92, 191);
    private static readonly Color Archive = Color.FromArgb(120, 130, 150);
    private static readonly Color Media = Color.FromArgb(206, 88, 155);
    private static readonly Color Book = Color.FromArgb(150, 110, 60);

    public static (string Label, Color Color) For(string path)
    {
        var extension = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
        return extension switch
        {
            "doc" or "docx" or "rtf" or "odt" or "docm" => ("W", Word),
            "xls" or "xlsx" or "xlsm" or "csv" or "tsv" or "ods" => ("X", Excel),
            "ppt" or "pptx" or "pptm" or "odp" => ("P", PowerPoint),
            "pdf" => ("PDF", Portable),
            "md" or "markdown" or "mdx" => ("Md", Markdown),
            "html" or "htm" or "xhtml" or "xml" => ("<>", Markup),
            "json" or "jsonl" or "yaml" or "yml" or "toml" => ("{}", Data),
            "txt" or "log" or "text" or "ini" or "cfg" => ("T", Plain),
            "jpg" or "jpeg" or "png" or "gif" or "bmp" or "webp" or "tif" or "tiff" => ("IMG", Image),
            "zip" or "gz" or "tar" or "7z" or "rar" => ("ZIP", Archive),
            "mp3" or "wav" or "m4a" or "flac" or "ogg" => ("AUD", Media),
            "mp4" or "mov" or "avi" or "mkv" or "webm" => ("VID", Media),
            "epub" or "mobi" or "azw3" => ("E", Book),
            "ipynb" or "msg" or "eml" => ("D", Plain),
            _ => ("?", Plain),
        };
    }
}
