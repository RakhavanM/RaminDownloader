using RaminDownloader.Models;
using RaminDownloader.Services;

namespace RaminDownloader.Tests;

public sealed class DownloaderBehaviorTests
{
    [Fact]
    public void VideoHighestUsesMp4CompatibleBestStreams()
    {
        var options = new DownloadOptions(
            new Uri("https://www.youtube.com/watch?v=abc123"),
            DownloadType.Video,
            DownloadQuality.Highest,
            true);

        var args = YtDlpArgumentsBuilder.Build(options, "/tmp/downloads", "/tmp/tools");

        Assert.Contains("--format", args);
        Assert.Contains("bestvideo*[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]/best", args);
        Assert.Contains("--merge-output-format", args);
        Assert.Contains("mp4", args);
        Assert.Contains("--cookies-from-browser", args);
        Assert.Contains("firefox", args);
    }

    [Fact]
    public void AudioLowestUsesAudioExtractionAndLowQuality()
    {
        var options = new DownloadOptions(
            new Uri("https://example.com/video"),
            DownloadType.AudioMp3,
            DownloadQuality.Lowest,
            true);

        var args = YtDlpArgumentsBuilder.Build(options, "/tmp/downloads", "/tmp/tools");

        Assert.Contains("--extract-audio", args);
        Assert.Contains("--audio-format", args);
        Assert.Contains("mp3", args);
        Assert.Contains("--audio-quality", args);
        Assert.Contains("9", args);
        Assert.Contains("worstaudio/worst", args);
    }

    [Fact]
    public void InvalidUrlIsRejectedBeforeBuildingArguments()
    {
        Assert.False(DownloadOptions.TryCreate("not-a-url", DownloadType.Video, DownloadQuality.Highest, true, out _));
        Assert.True(DownloadOptions.TryCreate("https://example.com/watch", DownloadType.Video, DownloadQuality.Highest, true, out _));
    }

    [Theory]
    [InlineData("[download]  42.5% of 10.00MiB at 2.00MiB/s ETA 00:03", 42.5)]
    [InlineData("[download] 100% of 10.00MiB in 00:05", 100)]
    public void ProgressParserReadsDownloadPercentage(string line, double expected)
    {
        var update = ProgressParser.Parse(line);
        Assert.NotNull(update);
        Assert.NotNull(update!.Percent);
        Assert.Equal(expected, update.Percent!.Value, precision: 1);
    }
}
