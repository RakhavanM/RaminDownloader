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
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationDirectory);

        var candidates = new[]
        {
            Path.Combine(applicationDirectory, "tools", "win-x64"),
            Path.Combine(applicationDirectory, "tools"),
            applicationDirectory
        };

        foreach (var directory in candidates)
        {
            var paths = new
            {
                Directory = directory,
                YtDlp = Path.Combine(directory, "yt-dlp.exe"),
                Ffmpeg = Path.Combine(directory, "ffmpeg.exe"),
                Ffprobe = Path.Combine(directory, "ffprobe.exe"),
                Deno = Path.Combine(directory, "deno.exe")
            };

            if (File.Exists(paths.YtDlp) &&
                File.Exists(paths.Ffmpeg) &&
                File.Exists(paths.Ffprobe) &&
                File.Exists(paths.Deno))
            {
                return new BundledTools(
                    paths.Directory,
                    paths.YtDlp,
                    paths.Ffmpeg,
                    paths.Ffprobe,
                    paths.Deno);
            }
        }

        throw new FileNotFoundException(
            "The bundled yt-dlp, FFmpeg, FFprobe, and Deno files were not found. Re-extract the complete application package.");
    }
}
