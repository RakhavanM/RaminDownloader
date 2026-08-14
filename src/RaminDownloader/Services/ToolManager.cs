using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RaminDownloader.Services;

public sealed record ToolStatus(string Name, string? Path, string? Version, bool Installed, string Message);

public sealed class ToolManager
{
    private readonly HttpClient _httpClient;
    private readonly string _applicationDirectory;
    private readonly string _toolDirectory;
    private readonly string _manifestPath;

    public ToolManager(string applicationDirectory, HttpClient? httpClient = null)
    {
        _applicationDirectory = applicationDirectory;
        _toolDirectory = Path.Combine(applicationDirectory, "Assets", "tools");
        _manifestPath = Path.Combine(applicationDirectory, "Assets", "tools-manifest.json");
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("RaminDownloader-ToolManager/1.0");
    }

    public IReadOnlyList<ToolStatus> GetInstalledStatus()
    {
        var result = new List<ToolStatus>();
        foreach (var name in new[] { "yt-dlp.exe", "ffmpeg.exe", "ffprobe.exe", "deno.exe" })
        {
            var path = ToolLocator.FindExecutable(_applicationDirectory, name);
            result.Add(new ToolStatus(name, path, null, path is not null, path is null ? "Not installed" : "Installed"));
        }
        return result;
    }

    public async Task<IReadOnlyList<ToolStatus>> EnsureToolsAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        if (ToolLocator.TryLocate(_applicationDirectory, out _)) return GetInstalledStatus();
        return await UpdateAsync(progress, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ToolStatus>> UpdateAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_toolDirectory);
        var manifest = await LoadManifestAsync(cancellationToken).ConfigureAwait(false);
        var staging = Path.Combine(Path.GetTempPath(), "RaminDownloader-tools-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(staging);

        try
        {
            foreach (var tool in manifest.Tools)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report($"Downloading {tool.Name}...");
                var archivePath = Path.Combine(staging, Path.GetFileName(new Uri(tool.DownloadUrl).AbsolutePath));
                await DownloadAsync(tool.DownloadUrl, archivePath, progress, cancellationToken).ConfigureAwait(false);
                var expectedHash = await ReadExpectedHashAsync(tool, archivePath, cancellationToken).ConfigureAwait(false);
                var actualHash = await ComputeSha256Async(archivePath, cancellationToken).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(expectedHash))
                    throw new InvalidDataException($"No checksum was found for {tool.Name}.");
                if (!actualHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException($"Hash verification failed for {tool.Name}.");

                if (tool.OutputNames.Count == 1 && tool.OutputNames[0].Equals("yt-dlp.exe", StringComparison.OrdinalIgnoreCase))
                {
                    InstallFileAtomically(archivePath, Path.Combine(_toolDirectory, "yt-dlp.exe"));
                }
                else
                {
                    var extractDirectory = Path.Combine(staging, tool.Name + "-extract");
                    ZipFile.ExtractToDirectory(archivePath, extractDirectory, overwriteFiles: true);
                    foreach (var outputName in tool.OutputNames)
                    {
                        var extracted = Directory.EnumerateFiles(extractDirectory, outputName, SearchOption.AllDirectories).FirstOrDefault();
                        if (extracted is null) throw new FileNotFoundException($"{outputName} was not found in the downloaded {tool.Name} archive.");
                        InstallFileAtomically(extracted, Path.Combine(_toolDirectory, outputName));
                    }
                }
                progress?.Report($"Installed {tool.Name}.");
            }
            return GetInstalledStatus();
        }
        finally
        {
            try { Directory.Delete(staging, recursive: true); } catch { }
        }
    }

    private async Task<ToolManifest> LoadManifestAsync(CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(_manifestPath);
        return await JsonSerializer.DeserializeAsync<ToolManifest>(stream, cancellationToken: cancellationToken)
            ?? throw new InvalidDataException("The tool manifest is empty or invalid.");
    }

    private async Task DownloadAsync(string url, string destination, IProgress<string>? progress, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var output = File.Create(destination);
        await input.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
        progress?.Report($"Downloaded {Path.GetFileName(destination)}.");
    }

    private async Task<string?> ReadExpectedHashAsync(ToolManifestTool tool, string archivePath, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(tool.Sha256Url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var fileName = Path.GetFileName(new Uri(tool.DownloadUrl).AbsolutePath);
        var match = Regex.Match(text, $"(?im)^([a-f0-9]{{64}})\\s+[* ]?{Regex.Escape(fileName)}\\s*$");
        if (match.Success) return match.Groups[1].Value;
        match = Regex.Match(text, "(?im)\\b([a-f0-9]{64})\\b");
        return match.Success ? match.Groups[1].Value : null;
    }

    private static async Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static void InstallFileAtomically(string source, string destination)
    {
        var temporary = destination + ".new";
        File.Copy(source, temporary, overwrite: true);
        File.Move(temporary, destination, overwrite: true);
    }
}

public sealed class ToolManifest
{
    public int SchemaVersion { get; set; }
    public List<ToolManifestTool> Tools { get; set; } = new();
}

public sealed class ToolManifestTool
{
    public string Name { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public string Sha256Url { get; set; } = string.Empty;
    public List<string> OutputNames { get; set; } = new();
}
