using System.Diagnostics;

namespace RaminDownloader.Desktop;

internal static class ToolProcess
{
    public static string Directory => Path.Combine(AppContext.BaseDirectory, "tools");

    public static async Task<int> RunAsync(string tool, IEnumerable<string> arguments,
        Action<string>? stdout, Action<string>? stderr, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        var executable = Path.Combine(Directory, tool + (OperatingSystem.IsWindows() ? ".exe" : ""));
        if (!File.Exists(executable))
            throw new IOException($"The bundled {tool} is missing. Reinstall Ramin Downloader.");

        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in arguments)
            start.ArgumentList.Add(argument);

        using var process = Process.Start(start) ?? throw new IOException($"Could not start {tool}.");
        using var registration = cancellation.Register(() =>
        {
            try { process.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) { }
            catch (System.ComponentModel.Win32Exception) { }
        });

        await Task.WhenAll(ReadAsync(process.StandardOutput, stdout),
            ReadAsync(process.StandardError, stderr), process.WaitForExitAsync()).ConfigureAwait(false);
        cancellation.ThrowIfCancellationRequested();
        return process.ExitCode;
    }

    private static async Task ReadAsync(StreamReader reader, Action<string>? receive)
    {
        while (await reader.ReadLineAsync().ConfigureAwait(false) is { } line)
            receive?.Invoke(line);
    }
}
