using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MarkItDownDesktop;

internal sealed class EngineUpdateService
{
    private const string ManifestUrl = "https://github.com/JiafuYuan/MarkItDownDesktop/releases/latest/download/engine-update.json";
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromMinutes(10) };
    private static readonly Regex VersionPattern = new(@"\d+\.\d+(?:\.\d+)?", RegexOptions.Compiled);
    private readonly string enginePath;
    private readonly string versionPath;

    public EngineUpdateService(string enginePath)
    {
        this.enginePath = enginePath;
        versionPath = Path.Combine(Path.GetDirectoryName(enginePath) ?? ".", "engine.version");
    }

    public async Task<EngineRelease> GetLatestReleaseAsync(CancellationToken cancellationToken)
    {
        using var response = await HttpClient.GetAsync(ManifestUrl, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new EngineUpdateException("尚未发布可下载的引擎版本。请先在 GitHub Releases 发布引擎。 ");
        }

        response.EnsureSuccessStatusCode();
        var manifest = await JsonSerializer.DeserializeAsync<EngineRelease>(
            await response.Content.ReadAsStreamAsync(cancellationToken),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
            cancellationToken: cancellationToken);

        if (manifest is null || string.IsNullOrWhiteSpace(manifest.Version) ||
            !Uri.TryCreate(manifest.Url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
            !Regex.IsMatch(manifest.Sha256 ?? string.Empty, "^[a-fA-F0-9]{64}$"))
        {
            throw new EngineUpdateException("GitHub 上的引擎更新描述文件格式无效。");
        }

        return manifest;
    }

    public async Task<string?> GetInstalledVersionAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(enginePath)) return null;

        // engine.version 记录的是本程序发布的引擎版本，优先于引擎自报版本。
        // markitdown.exe --version 输出的是上游 markitdown 包版本，可能落后于我们的引擎版本。
        var recorded = await ReadRecordedVersionAsync(cancellationToken);
        if (recorded is not null) return recorded;

        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo(enginePath, "--version")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                },
            };
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            var match = VersionPattern.Match(output);
            return process.ExitCode == 0 && match.Success ? match.Value : null;
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            // 引擎文件损坏或无法执行时按“版本未知”处理，使用户可以重新下载安装。
            return null;
        }
    }

    public async Task InstallAsync(EngineRelease release, IProgress<int>? progress, CancellationToken cancellationToken)
    {
        var temporaryPath = enginePath + ".download";
        try
        {
            using var response = await HttpClient.GetAsync(release.Url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            var length = response.Content.Headers.ContentLength;
            await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
            {
                await using var output = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None);
                var buffer = new byte[1024 * 128];
                long downloaded = 0;
                int read;
                while ((read = await input.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                    downloaded += read;
                    if (length is > 0) progress?.Report((int)(downloaded * 100 / length.Value));
                }

                await output.FlushAsync(cancellationToken);
            }
            var actualHash = await GetSha256Async(temporaryPath, cancellationToken);
            if (!string.Equals(actualHash, release.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new EngineUpdateException("下载文件的 SHA-256 校验失败，已取消更新。 ");
            }

            var backupPath = enginePath + ".previous";
            if (File.Exists(enginePath))
            {
                File.Replace(temporaryPath, enginePath, backupPath, true);
                File.Delete(backupPath);
            }
            else
            {
                File.Move(temporaryPath, enginePath);
            }

            await File.WriteAllTextAsync(versionPath, release.Version + Environment.NewLine, cancellationToken);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    private async Task<string?> ReadRecordedVersionAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(versionPath)) return null;
        try
        {
            var text = (await File.ReadAllTextAsync(versionPath, cancellationToken)).Trim();
            return Version.TryParse(text, out _) ? text : null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static async Task<string> GetSha256Async(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

internal sealed class EngineUpdateException(string message) : Exception(message);

internal sealed record EngineRelease(string Version, string Url, string Sha256)
{
    public bool IsNewerThan(string? installedVersion)
    {
        if (string.IsNullOrWhiteSpace(installedVersion)) return true;
        return System.Version.TryParse(Version, out var latest) && System.Version.TryParse(installedVersion, out var installed)
            ? latest > installed
            : !string.Equals(Version, installedVersion, StringComparison.OrdinalIgnoreCase);
    }
}
