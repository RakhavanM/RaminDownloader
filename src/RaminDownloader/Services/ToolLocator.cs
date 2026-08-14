using System.IO;

namespace RaminDownloader.Services;

public sealed record BundledTools(
    string Directory,
    string YtDlp,
    string Ffmpeg,
    string Ffprobe,
    string Deno);

public static class ToolLocator
{
    public static BundledTools Locate(string applicationDirectory)
    {
        if (TryLocate(applicationDirectory, out var tools))
        {
            return tools;
        }

        throw new FileNotFoundException("Required tools are not installed. Click Update to download yt-dlp, FFmpeg, FFprobe, and Deno.");
    }

    public static bool TryLocate(string applicationDirectory, out BundledTools tools)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationDirectory);

        var ytDlp = FindExecutable(applicationDirectory, "yt-dlp.exe");
        var ffmpeg = FindExecutable(applicationDirectory, "ffmpeg.exe");
        var ffprobe = FindExecutable(applicationDirectory, "ffprobe.exe");
        var deno = FindExecutable(applicationDirectory, "deno.exe");

        if (ytDlp is not null && ffmpeg is not null && ffprobe is not null && deno is not null)
        {
            tools = new BundledTools(
                Path.GetDirectoryName(ffmpeg) ?? applicationDirectory,
                ytDlp,
                ffmpeg,
                ffprobe,
                deno);
            return true;
        }

        tools = null!;
        return false;
    }

    public static string? FindExecutable(string applicationDirectory, string name)
    {
        var candidates = new[]
        {
            Path.Combine(applicationDirectory, "Assets", "tools", name),
            Path.Combine(applicationDirectory, "tools", "win-x64", name),
            Path.Combine(applicationDirectory, "tools", name),
            Path.Combine(applicationDirectory, name)
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return Path.GetFullPath(candidate);
            }
        }

        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var candidate = Path.Combine(directory.Trim('"'), name);
            if (File.Exists(candidate))
            {
                return Path.GetFullPath(candidate);
            }
        }

        return null;
    }
}
