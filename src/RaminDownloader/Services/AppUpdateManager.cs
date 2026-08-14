using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RaminDownloader.Services;

public sealed record AppUpdateInfo(
    Version CurrentVersion,
    Version LatestVersion,
    string TagName,
    string DownloadUrl,
    string? ChecksumUrl);

public sealed class AppUpdateManager
{
    private const string LatestReleaseUrl = "https://api.github.com/repos/RakhavanM/RaminDownloader/releases/latest";
    private readonly HttpClient _httpClient;
    private readonly string _applicationDirectory;

    public AppUpdateManager(string applicationDirectory, HttpClient? httpClient = null)
    {
        _applicationDirectory = applicationDirectory;
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromMinutes(15) };
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("RaminDownloader-AppUpdater/1.0");
    }

    public async Task<AppUpdateInfo?> CheckAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(LatestReleaseUrl, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        var root = document.RootElement;
        var tagName = root.GetProperty("tag_name").GetString() ?? throw new InvalidDataException("GitHub release has no tag.");
        var latestVersion = ParseVersion(tagName);
        var currentVersion = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0, 0);
        if (latestVersion <= currentVersion) return null;

        string? downloadUrl = null;
        string? checksumUrl = null;
        foreach (var asset in root.GetProperty("assets").EnumerateArray())
        {
            var name = asset.GetProperty("name").GetString() ?? string.Empty;
            var url = asset.GetProperty("browser_download_url").GetString();
            if (name.Equals("RaminDownloader-win-x64.zip", StringComparison.OrdinalIgnoreCase)) downloadUrl = url;
            if (name.Equals("SHA256SUMS.txt", StringComparison.OrdinalIgnoreCase)) checksumUrl = url;
        }

        if (string.IsNullOrWhiteSpace(downloadUrl))
            throw new InvalidDataException("The latest GitHub release has no Windows x64 ZIP asset.");

        return new AppUpdateInfo(currentVersion, latestVersion, tagName, downloadUrl!, checksumUrl);
    }

    public async Task DownloadAndScheduleAsync(
        AppUpdateInfo update,
        string executableName,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!CanWriteToApplicationDirectory())
            throw new UnauthorizedAccessException("The application folder is not writable. Move RaminDownloader to a user-writable folder before updating.");

        var tempRoot = Path.Combine(Path.GetTempPath(), "RaminDownloader-update-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        var zipPath = Path.Combine(tempRoot, "RaminDownloader-win-x64.zip");
        var checksumPath = Path.Combine(tempRoot, "SHA256SUMS.txt");
        var scriptPath = Path.Combine(tempRoot, "apply-update.ps1");

        try
        {
            progress?.Report($"Downloading RaminDownloader {update.TagName}...");
            await DownloadFileAsync(update.DownloadUrl, zipPath, progress, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(update.ChecksumUrl))
            {
                await DownloadFileAsync(update.ChecksumUrl!, checksumPath, progress, cancellationToken).ConfigureAwait(false);
                await VerifyChecksumAsync(zipPath, checksumPath, cancellationToken).ConfigureAwait(false);
            }

            var stageDirectory = Path.Combine(tempRoot, "stage");
            var script = BuildUpdateScript(
                tempRoot,
                zipPath,
                stageDirectory,
                _applicationDirectory,
                executableName,
                Environment.ProcessId);
            await File.WriteAllTextAsync(scriptPath, script, cancellationToken).ConfigureAwait(false);

            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                UseShellExecute = true,
                WorkingDirectory = _applicationDirectory,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            startInfo.ArgumentList.Add("-NoLogo");
            startInfo.ArgumentList.Add("-NoProfile");
            startInfo.ArgumentList.Add("-ExecutionPolicy");
            startInfo.ArgumentList.Add("Bypass");
            startInfo.ArgumentList.Add("-File");
            startInfo.ArgumentList.Add(scriptPath);
            var helper = Process.Start(startInfo);
            if (helper is null) throw new InvalidOperationException("Could not start the update helper.");
            progress?.Report("Update is ready. The application will restart now.");
        }
        catch
        {
            try { Directory.Delete(tempRoot, recursive: true); } catch { }
            throw;
        }
    }

    private async Task DownloadFileAsync(string url, string destination, IProgress<string>? progress, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var output = File.Create(destination);
        await input.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
        progress?.Report($"Downloaded {Path.GetFileName(destination)}.");
    }

    private static async Task VerifyChecksumAsync(string zipPath, string checksumPath, CancellationToken cancellationToken)
    {
        var expectedText = await File.ReadAllTextAsync(checksumPath, cancellationToken).ConfigureAwait(false);
        var match = Regex.Match(expectedText, "(?im)^([a-f0-9]{64})\\s+.*RaminDownloader-win-x64\\.zip\\s*$");
        if (!match.Success) throw new InvalidDataException("The release checksum file does not contain the application ZIP checksum.");
        await using var stream = File.OpenRead(zipPath);
        using var sha = SHA256.Create();
        var actual = Convert.ToHexString(await sha.ComputeHashAsync(stream, cancellationToken).ConfigureAwait(false)).ToLowerInvariant();
        if (!actual.Equals(match.Groups[1].Value, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("The downloaded application ZIP failed checksum verification.");
    }

    private static Version ParseVersion(string tagName)
    {
        var value = tagName.Trim().TrimStart('v', 'V');
        var separator = value.IndexOfAny(new[] { '-', '+' });
        if (separator >= 0) value = value[..separator];
        return Version.Parse(value);
    }

    private bool CanWriteToApplicationDirectory()
    {
        try
        {
            var testPath = Path.Combine(_applicationDirectory, ".update-write-test-" + Guid.NewGuid().ToString("N"));
            File.WriteAllText(testPath, string.Empty);
            File.Delete(testPath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string BuildUpdateScript(
        string tempRoot,
        string zipPath,
        string stageDirectory,
        string installDirectory,
        string executableName,
        int processId)
    {
        var lines = new[]
        {
            "$ErrorActionPreference = 'Stop'",
            $"$tempRoot = {PowerShellLiteral(tempRoot)}",
            $"$zipPath = {PowerShellLiteral(zipPath)}",
            $"$stageDirectory = {PowerShellLiteral(stageDirectory)}",
            $"$installDirectory = {PowerShellLiteral(installDirectory)}",
            $"$executableName = {PowerShellLiteral(executableName)}",
            $"while (Get-Process -Id {processId} -ErrorAction SilentlyContinue) {{ Start-Sleep -Milliseconds 250 }}",
            "Expand-Archive -LiteralPath $zipPath -DestinationPath $stageDirectory -Force",
            "$sourceDirectory = Join-Path $stageDirectory 'RaminDownloader'",
            "if (-not (Test-Path -LiteralPath $sourceDirectory)) { $sourceDirectory = $stageDirectory }",
            "Get-ChildItem -LiteralPath $sourceDirectory -Force | Copy-Item -Destination $installDirectory -Recurse -Force",
            "Start-Process -FilePath (Join-Path $installDirectory $executableName)",
            "try { Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue } catch { }"
        };
        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }

    private static string PowerShellLiteral(string value) => "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";
}
