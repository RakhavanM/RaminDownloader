using System.Globalization;
using System.Text.Json;
using RaminDownloader.Models;
using RaminDownloader.Services;

namespace RaminDownloader.Desktop;

internal enum MediaAction { Resize, Compress, Convert }

internal sealed record MediaOptions(MediaAction Action, string Format, int Height = 720, int Crf = 25);

internal static class MediaJobs
{
    public static readonly string[] Formats = ["MP4", "MKV", "WebM", "MP3", "M4A", "Opus", "WAV", "FLAC"];

    public static async Task DownloadAsync(DownloadOptions options, string? firefoxProfile,
        string folder, IProgress<double> progress, CancellationToken cancellation)
    {
        try { Directory.CreateDirectory(folder); }
        catch (Exception exception) when (JobErrors.IsFileError(exception))
        {
            throw JobErrors.Storage(exception);
        }
        var deno = Path.Combine(ToolProcess.Directory, OperatingSystem.IsWindows() ? "deno.exe" : "deno");
        var args = new List<string> { "--ignore-config", "--progress", "--no-colors" };
        args.AddRange(YtDlpArgumentsBuilder.Build(options, folder, ToolProcess.Directory, deno, firefoxProfile));
        args.Insert(args.Count - 1, "--");

        var error = new ToolDiagnostics();
        var exit = await ToolProcess.RunAsync("yt-dlp", args, line =>
        {
            if (line.StartsWith("[download]") && ProgressParser.Parse(line)?.Percent is { } percent)
                progress.Report(Math.Min(percent, 99));
        }, error.Append, cancellation);

        if (exit != 0)
            throw JobErrors.Download(exit, error.ToString(), options.UseFirefoxCookies);
    }

    public static async Task ProcessAsync(string input, string output, MediaOptions options,
        IProgress<double> progress, CancellationToken cancellation)
    {
        try
        {
            using var inputFile = File.Open(input, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }
        catch (Exception exception) when (JobErrors.IsFileError(exception))
        {
            throw new JobError(Failure.Input,
                "The input file could not be opened. Check that it still exists and that you can read it, then choose it again.", exception.Message, exception);
        }
        if (File.Exists(output))
            throw OutputExists(output);

        var destinationFolder = Path.GetDirectoryName(output)!;
        try
        {
            if (!File.GetAttributes(destinationFolder).HasFlag(FileAttributes.Directory))
                throw new DirectoryNotFoundException(destinationFolder);
        }
        catch (FileNotFoundException exception)
        {
            throw JobErrors.Storage(new DirectoryNotFoundException(destinationFolder, exception));
        }
        catch (Exception exception) when (JobErrors.IsFileError(exception))
        {
            throw JobErrors.Storage(exception);
        }

        var info = await ProbeAsync(input, cancellation);
        var audioOnly = options.Format is "MP3" or "M4A" or "Opus" or "WAV" or "FLAC";
        if (audioOnly ? !info.Audio : !info.Video)
            throw new JobError(Failure.Input, audioOnly ? "The input has no audio track. Choose a file with sound." :
                "The input has no video track. Choose a video file or convert it to an audio format.");

        // Keep unfinished outputs beside their destination so the final move is atomic.
        var temporary = Path.Combine(destinationFolder,
            $".ramin-{Guid.NewGuid():N}{Path.GetExtension(output)}");
        try
        {
            var args = new List<string> { "-hide_banner", "-nostdin", "-n", "-v", "error",
                "-progress", "pipe:1", "-i", input };
            if (audioOnly)
                args.AddRange(["-map", "0:a:0", "-vn"]);
            else
                args.AddRange(["-map", "0:V:0", "-map", "0:a:0?"]);

            if (!audioOnly)
            {
                if (options.Format == "WebM")
                    args.AddRange(["-c:v", "libvpx-vp9", "-b:v", "0", "-crf", "32", "-pix_fmt", "yuv420p"]);
                else
                    args.AddRange(["-c:v", "libx264", "-preset", "medium", "-crf",
                        options.Crf.ToString(CultureInfo.InvariantCulture), "-pix_fmt", "yuv420p",
                        "-vf", options.Action == MediaAction.Resize
                            ? $"scale=-2:'min(ih,{options.Height})',pad=ceil(iw/2)*2:ceil(ih/2)*2"
                            : "pad=ceil(iw/2)*2:ceil(ih/2)*2"]);
            }
            args.AddRange(["-c:a", AudioEncoder(options.Format)]);
            if (options.Format is "MP4" or "M4A")
                args.AddRange(["-movflags", "+faststart"]);
            args.Add(temporary);

            var error = new ToolDiagnostics();
            var exit = await ToolProcess.RunAsync("ffmpeg", args, line =>
            {
                if (info.Duration > 0 && line.StartsWith("out_time_us=", StringComparison.Ordinal) &&
                    long.TryParse(line.AsSpan(12), CultureInfo.InvariantCulture, out var microseconds))
                    progress.Report(Math.Clamp(microseconds / (info.Duration * 10_000), 0, 99));
            }, error.Append, cancellation);
            if (exit != 0 || !File.Exists(temporary))
                throw JobErrors.Encode(exit, error.ToString());
            cancellation.ThrowIfCancellationRequested();
            try { File.Move(temporary, output); }
            catch (IOException) when (File.Exists(output)) { throw OutputExists(output); }
            catch (Exception exception) when (JobErrors.IsFileError(exception))
            {
                throw JobErrors.Storage(exception);
            }
        }
        finally
        {
            // Cleanup failure must not replace the original failure or cancellation.
            try { File.Delete(temporary); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private static JobError OutputExists(string path) => new(Failure.Storage,
        "A file already exists at the output path. Choose a different filename.", path);

    private static string AudioEncoder(string format) => format switch
    {
        "MP3" => "libmp3lame",
        "WebM" or "Opus" => "libopus",
        "WAV" => "pcm_s16le",
        "FLAC" => "flac",
        _ => "aac"
    };

    private static async Task<(double Duration, bool Video, bool Audio)> ProbeAsync(
        string input, CancellationToken cancellation)
    {
        var json = new System.Text.StringBuilder();
        var error = new ToolDiagnostics();
        var exit = await ToolProcess.RunAsync("ffprobe",
            ["-v", "error", "-show_entries", "format=duration:stream=codec_type:stream_disposition=attached_pic",
                "-of", "json", "-i", input], line => json.AppendLine(line), error.Append, cancellation);
        if (exit != 0)
            throw new JobError(Failure.Input,
                "The selected file could not be read as audio or video. Check that it plays in a media player or choose another file.",
                JobErrors.ToolDetails("FFprobe", exit, error.ToString()));

        using var document = JsonDocument.Parse(json.ToString());
        var root = document.RootElement;
        double duration = 0;
        if (root.TryGetProperty("format", out var format) && format.TryGetProperty("duration", out var seconds))
            double.TryParse(seconds.GetString(), CultureInfo.InvariantCulture, out duration);
        bool video = false, audio = false;
        foreach (var stream in root.GetProperty("streams").EnumerateArray())
        {
            var type = stream.GetProperty("codec_type").GetString();
            audio |= type == "audio";
            video |= type == "video" && (!stream.TryGetProperty("disposition", out var disposition) ||
                !disposition.TryGetProperty("attached_pic", out var picture) || picture.GetInt32() == 0);
        }
        return (duration, video, audio);
    }
}
