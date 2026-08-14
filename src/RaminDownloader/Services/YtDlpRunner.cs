using System.Diagnostics;
using System.IO;
using RaminDownloader.Models;

namespace RaminDownloader.Services;

public sealed class YtDlpRunner
{
    public async Task<int> RunAsync(
        DownloadOptions options,
        string applicationDirectory,
        string downloadDirectory,
        IProgress<DownloadProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        Directory.CreateDirectory(downloadDirectory);

        var tools = ToolLocator.Locate(applicationDirectory);
        var arguments = YtDlpArgumentsBuilder.Build(options, downloadDirectory, tools.Directory, tools.Deno);
        var startInfo = new ProcessStartInfo
        {
            FileName = tools.YtDlp,
            WorkingDirectory = applicationDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        if (!process.Start())
        {
            throw new InvalidOperationException("yt-dlp could not be started.");
        }

        using var cancellationRegistration = cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (InvalidOperationException)
            {
                // The process exited between HasExited and Kill.
            }
        });

        var stdoutTask = ReadLinesAsync(process.StandardOutput, progress);
        var stderrTask = ReadLinesAsync(process.StandardError, progress);
        var exitTask = process.WaitForExitAsync();

        await Task.WhenAll(stdoutTask, stderrTask, exitTask).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return process.ExitCode;
    }

    private static async Task ReadLinesAsync(
        StreamReader reader,
        IProgress<DownloadProgress>? progress)
    {
        while (await reader.ReadLineAsync().ConfigureAwait(false) is { } line)
        {
            progress?.Report(ProgressParser.Parse(line) ?? new DownloadProgress(null, line));
        }
    }
}
