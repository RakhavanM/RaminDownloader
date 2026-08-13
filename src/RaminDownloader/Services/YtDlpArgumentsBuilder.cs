using System.IO;
using RaminDownloader.Models;

namespace RaminDownloader.Services;

public static class YtDlpArgumentsBuilder
{
    public static IReadOnlyList<string> Build(
        DownloadOptions options,
        string downloadDirectory,
        string toolsDirectory)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(downloadDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(toolsDirectory);

        var arguments = new List<string>
        {
            "--no-playlist",
            "--windows-filenames",
            "--no-overwrites",
            "--newline",
            "--retries", "10",
            "--fragment-retries", "10",
            "--sleep-requests", "1",
            "--output", Path.Combine(downloadDirectory, "%(title)s [%(id)s].%(ext)s"),
            "--ffmpeg-location", toolsDirectory,
            "--js-runtimes", $"deno:{Path.Combine(toolsDirectory, "deno.exe")}",
            "--merge-output-format", "mp4"
        };

        if (options.UseFirefoxCookies)
        {
            arguments.Add("--cookies-from-browser");
            arguments.Add("firefox");
        }

        if (options.Type == DownloadType.Video)
        {
            arguments.Add("--format");
            arguments.Add(VideoFormat(options.Quality));
        }
        else
        {
            arguments.Add("--format");
            arguments.Add(AudioFormat(options.Quality));
            arguments.Add("--extract-audio");
            arguments.Add("--audio-format");
            arguments.Add("mp3");
            arguments.Add("--audio-quality");
            arguments.Add(AudioQuality(options.Quality));
        }

        // The URL is always the final argument and is never concatenated into a shell command.
        arguments.Add(options.Url.ToString());
        return arguments;
    }

    private static string VideoFormat(DownloadQuality quality) => quality switch
    {
        DownloadQuality.Highest => "bestvideo*[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]/best",
        DownloadQuality.Medium => "bestvideo*[height<=720][ext=mp4]+bestaudio[ext=m4a]/bestvideo*[height<=720]+bestaudio/best[height<=720][ext=mp4]/best[height<=720]/best",
        DownloadQuality.Lowest => "worstvideo*[ext=mp4]+worstaudio[ext=m4a]/worst[ext=mp4]/worst",
        _ => throw new ArgumentOutOfRangeException(nameof(quality), quality, null)
    };

    private static string AudioQuality(DownloadQuality quality) => quality switch
    {
        DownloadQuality.Highest => "0",
        DownloadQuality.Medium => "5",
        DownloadQuality.Lowest => "9",
        _ => throw new ArgumentOutOfRangeException(nameof(quality), quality, null)
    };

    private static string AudioFormat(DownloadQuality quality) => quality switch
    {
        DownloadQuality.Highest => "bestaudio/best",
        DownloadQuality.Medium => "bestaudio[abr<=128]/bestaudio/best",
        DownloadQuality.Lowest => "worstaudio/worst",
        _ => throw new ArgumentOutOfRangeException(nameof(quality), quality, null)
    };
}
