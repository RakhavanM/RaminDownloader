using System.Text;
using RaminDownloader.Desktop;

using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(2));
var token = deadline.Token;
var folder = Path.Combine(Path.GetTempPath(), $"Ramin Smoke {Guid.NewGuid():N}");
Directory.CreateDirectory(folder);
try
{
    var source = Path.Combine(folder, "نمونه video.mp4");
    await Run("ffmpeg", "-v", "error", "-f", "lavfi", "-i", "testsrc2=size=320x240:rate=10",
        "-f", "lavfi", "-i", "sine=440", "-t", "1", "-c:v", "libx264", "-c:a", "aac", source);

    var resized = Path.Combine(folder, "resized.mp4");
    await MediaJobs.ProcessAsync(source, resized, new(MediaAction.Resize, "MP4", 120), new Progress<double>(), token);
    if (await Probe(resized, "height", "v:0") != "120")
        throw new InvalidOperationException("Resize did not produce the expected height.");

    var compressed = Path.Combine(folder, "compressed.mp4");
    await MediaJobs.ProcessAsync(source, compressed, new(MediaAction.Compress, "MP4", Crf: 32), new Progress<double>(), token);
    if (await Probe(compressed, "height", "v:0") != "240" || new FileInfo(compressed).Length >= new FileInfo(source).Length)
        throw new InvalidOperationException("Compression did not preserve height and reduce the sample size.");

    var audio = Path.Combine(folder, "audio.mp3");
    await MediaJobs.ProcessAsync(source, audio, new(MediaAction.Convert, "MP3"), new Progress<double>(), token);
    if (await Probe(audio, "codec_name", "a:0") != "mp3")
        throw new InvalidOperationException("The converted audio is not MP3.");

    var video = Path.Combine(folder, "video.webm");
    await MediaJobs.ProcessAsync(source, video, new(MediaAction.Convert, "WebM"), new Progress<double>(), token);
    if (await Probe(video, "codec_name", "v:0") != "vp9")
        throw new InvalidOperationException("The converted video is not VP9.");

    await Run("yt-dlp", "--ignore-config", "--version");
    await Run("deno", "--version");
    Console.WriteLine("Resize, compression, audio and video conversion, and bundled tool startup passed.");
}
finally
{
    Directory.Delete(folder, recursive: true);
}

Task<string> Probe(string path, string field, string track) =>
    Run("ffprobe", "-v", "error", "-select_streams", track, "-show_entries", $"stream={field}",
        "-of", "csv=p=0", path);

async Task<string> Run(string tool, params string[] arguments)
{
    var output = new StringBuilder();
    var error = new StringBuilder();
    var exit = await ToolProcess.RunAsync(tool, arguments,
        line => output.AppendLine(line), line => error.AppendLine(line), token);
    if (exit != 0)
        throw new InvalidOperationException($"{tool} failed: {error}");
    return output.ToString().Trim();
}
