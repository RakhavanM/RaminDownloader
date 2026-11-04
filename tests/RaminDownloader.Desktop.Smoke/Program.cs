using System.Net;
using System.Net.Sockets;
using System.Text;
using RaminDownloader.Models;
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

    var progress = new Progress<double>();
    var resize = new MediaOptions(MediaAction.Resize, "MP4", 120);
    await Fails(Failure.Input, () => MediaJobs.ProcessAsync(Path.Combine(folder, "missing.mp4"),
        Path.Combine(folder, "unused.mp4"), resize, progress, token), "input file");
    var corrupt = Path.Combine(folder, "corrupt.mp4");
    await File.WriteAllTextAsync(corrupt, "not a video", token);
    await Fails(Failure.Input, () => MediaJobs.ProcessAsync(corrupt,
        Path.Combine(folder, "unused.mp4"), resize, progress, token), "media player", "FFprobe");
    await Fails(Failure.Input, () => MediaJobs.ProcessAsync(audio,
        Path.Combine(folder, "unused.mp4"), resize, progress, token), "no video track");
    await Fails(Failure.StorageMissing, () => MediaJobs.ProcessAsync(source,
        Path.Combine(folder, "missing folder", "unused.mp4"), resize, progress, token), "choose a different");
    await Fails(Failure.Storage, () => MediaJobs.ProcessAsync(source, resized, resize, progress, token),
        "different filename");
    if (await Probe(resized, "height", "v:0") != "120")
        throw new InvalidOperationException("An existing output was replaced.");
    if (Directory.EnumerateFiles(folder, ".ramin-*").Any())
        throw new InvalidOperationException("A failed job left a temporary output.");

    var url = new Uri("https://example.invalid/fixture.mp4");
    var download = new DownloadOptions(url, DownloadType.Video, DownloadQuality.Highest, true);
    var emptyProfile = Directory.CreateDirectory(Path.Combine(folder, "Firefox test profile")).FullName;
    await Fails(Failure.FirefoxCookies, () => MediaJobs.DownloadAsync(download, emptyProfile, folder, progress, token),
        "Choose the Firefox profile", "yt-dlp");
    await Fails(Failure.Storage, () => MediaJobs.DownloadAsync(download, null, source, progress, token),
        "destination");

    await HttpFailure("401 Unauthorized", Failure.SignIn, "enable", folder, token);
    await HttpFailure("503 Service Unavailable", Failure.Network, "internet or proxy", folder, token);
    Check(JobErrors.Download(1, "ERROR: Temporary failure in name resolution", false), Failure.Network, "retry", "name resolution");
    Check(JobErrors.Download(1, "ERROR: [SSL: CERTIFICATE_VERIFY_FAILED]", false), Failure.Network, "system date", "CERTIFICATE");
    Check(JobErrors.Download(1, "ERROR: Sign in to confirm you’re not a bot", false), Failure.SignIn, "enable", "Sign in to confirm");
    Check(JobErrors.Download(1, "ERROR: Sign in to confirm you’re not a bot", true), Failure.SignIn, "same profile", "Sign in to confirm");
    Check(JobErrors.Download(1, "ERROR: HTTP Error 403: Forbidden", false), Failure.SiteAccess, "available to you", "403");
    Check(JobErrors.Download(1, "ERROR: HTTP Error 429: Too Many Requests", false), Failure.SiteAccess, "Wait", "429");
    Check(JobErrors.Encode(1, "av_interleaved_write_frame(): No space left on device\nError writing trailer"), Failure.StorageFull, "Free up space", "No space left");
    Check(JobErrors.Storage(new UnauthorizedAccessException("denied")), Failure.StoragePermission, "folder you can write", "denied");
    Check(JobErrors.Picker(new IOException("Portal unavailable")), Failure.Picker, "drag it", "Portal unavailable");
    Check(JobErrors.LocalPath(), Failure.LocalPath, "local folder");
    await Run("yt-dlp", "--ignore-config", "--version");
    await Run("deno", "--version");
    Console.WriteLine("Resize, compression, audio and video conversion, bundled tool startup, and failure scenarios passed.");
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

static async Task Fails(Failure expected, Func<Task> operation, string guidance, string? details = null)
{
    try { await operation(); }
    catch (JobError error)
    {
        Check(error, expected, guidance, details);
        return;
    }
    throw new InvalidOperationException($"Expected {expected}, but the operation succeeded.");
}

static void Check(JobError error, Failure expected, string guidance, string? details = null)
{
    if (error.Kind != expected || !error.Message.Contains(guidance, StringComparison.OrdinalIgnoreCase) ||
        (details is not null && (error.Details?.Contains(details, StringComparison.OrdinalIgnoreCase) != true ||
                                 error.Message.Contains(details, StringComparison.OrdinalIgnoreCase))))
        throw new InvalidOperationException($"Expected {expected} with separate guidance and diagnostics. Got {error.Kind}: {error.Message} ({error.Details})");
}

static async Task HttpFailure(string status, Failure expected, string guidance, string folder, CancellationToken token)
{
    // Serve a local failure so the test does not depend on a real account or external site.
    using var server = new TcpListener(IPAddress.Loopback, 0);
    server.Start();
    using var stop = CancellationTokenSource.CreateLinkedTokenSource(token);
    var respond = RespondAsync();
    try
    {
        var url = new Uri($"http://127.0.0.1:{((IPEndPoint)server.LocalEndpoint).Port}/fixture.mp4");
        var options = new DownloadOptions(url, DownloadType.Video, DownloadQuality.Highest, false);
        await Fails(expected, () => MediaJobs.DownloadAsync(options, null, folder, new Progress<double>(), token),
            guidance, status[..3]);
    }
    finally
    {
        await stop.CancelAsync();
        try { await respond; }
        catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
    }

    async Task RespondAsync()
    {
        var response = Encoding.ASCII.GetBytes($"HTTP/1.1 {status}\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");
        while (!stop.IsCancellationRequested)
        {
            using var client = await server.AcceptTcpClientAsync(stop.Token);
            using var stream = client.GetStream();
            using var request = new StreamReader(stream, leaveOpen: true);
            while (await request.ReadLineAsync(stop.Token) is { Length: > 0 }) { }
            await stream.WriteAsync(response, stop.Token);
        }
    }
}
