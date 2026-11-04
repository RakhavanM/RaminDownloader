namespace RaminDownloader.Desktop;

internal enum Failure
{
    Network, SignIn, FirefoxCookies, SiteAccess, StorageFull, StoragePermission, StorageMissing,
    StorageBusy, Storage, Input, Picker, LocalPath, Clipboard, Tool, Download, Media, Unexpected
}

internal sealed class JobError(Failure kind, string guidance, string? details = null, Exception? cause = null)
    : Exception(guidance, cause)
{
    public Failure Kind { get; } = kind;
    public string? Details { get; } = details;
}

internal static class JobErrors
{
    public static JobError Download(int exit, string stderr, bool cookies)
    {
        var details = ToolDetails("yt-dlp", exit, stderr);
        if (cookies && Has(stderr, "failed to load cookies", "could not find firefox cookies",
                "could not copy firefox cookie", "could not extract cookies", "firefox cookies database",
                "cannot extract cookies from firefox", "cookies.sqlite", "database is locked"))
            return new(Failure.FirefoxCookies,
                "Firefox cookies could not be read. Choose the Firefox profile you signed in with. If Firefox has it locked, close Firefox and retry.", details);
        if (Has(stderr, "sign in", "log in", "login required", "login_required", "authentication required", "http error 401"))
            return new(Failure.SignIn, cookies
                ? "The site requires a valid sign-in. Open the link in Firefox, sign in or complete verification, then retry with the same profile."
                : "The site requires sign-in. Open the link in Firefox, sign in, then enable “Use my Firefox sign-in” and retry.", details);
        if (StorageFromTool(stderr, details) is { } storage)
            return storage;
        if (Has(stderr, "certificate verify failed", "certificate_verify_failed", "self-signed certificate"))
            return new(Failure.Network,
                "The secure connection could not be verified. Check your system date and time and your network or proxy settings, then retry.", details);
        if (Has(stderr, "timed out", "timeout", "name resolution", "getaddrinfo", "name or service not known",
                "nodename nor servname", "network is unreachable", "network unreachable", "connection refused",
                "connection reset", "connection aborted", "no route to host", "unable to connect", "remote end closed",
                "failed to resolve", "http error 502", "http error 503", "http error 504"))
            return new(Failure.Network,
                "The connection failed. Check that the link opens in your browser and that your internet or proxy is working, then retry.", details);
        if (Has(stderr, "http error 429", "too many requests"))
            return new(Failure.SiteAccess, "The site is limiting requests. Wait a few minutes before trying again.", details);
        if (Has(stderr, "http error 403", "http error 404", "video unavailable", "private video", "unsupported url"))
            return new(Failure.SiteAccess,
                "The site would not provide this media. Open the link in your browser and check that the item is available to you, then retry.", details);
        return new(Failure.Download, "The download failed. Check that the link still plays in your browser, then retry.", details);
    }

    public static JobError Encode(int exit, string stderr)
    {
        var details = ToolDetails("FFmpeg", exit, stderr);
        return StorageFromTool(stderr, details) ?? new(Failure.Media,
            "The file could not be processed. Try another source file or output format.", details);
    }

    public static JobError Storage(Exception exception)
    {
        // .NET can expose a native errno or a wrapped Win32 code in the low word.
        var code = exception.HResult & 0xffff;
        if (code == 112 || (OperatingSystem.IsWindows() ? code == 39 : code is 28 or 122))
            return StorageMessage(Failure.StorageFull, exception.Message, exception);
        if (exception is UnauthorizedAccessException || code is 5 or 13 || (OperatingSystem.IsWindows() ? code == 19 : code == 30))
            return StorageMessage(Failure.StoragePermission, exception.Message, exception);
        if (exception is DirectoryNotFoundException or DriveNotFoundException || (OperatingSystem.IsWindows() && code is 3 or 15))
            return StorageMessage(Failure.StorageMissing, exception.Message, exception);
        if (OperatingSystem.IsWindows() && code is 32 or 33)
            return StorageMessage(Failure.StorageBusy, exception.Message, exception);
        return StorageFromTool(exception.Message, exception.Message) ??
            StorageMessage(Failure.Storage, exception.Message, exception);
    }

    public static JobError Picker(Exception exception) => new(Failure.Picker,
        "The file or folder picker could not open. Try again. For an input file, you can also drag it from your file manager.", exception.Message, exception);

    public static JobError LocalPath() => new(Failure.LocalPath,
        "This selection has no local path. Copy the file to a folder on this device, or choose a local folder and try again.");

    public static JobError Tool(string tool, Exception exception) => new(Failure.Tool,
        $"The bundled {tool} could not start. Reinstall Ramin Downloader using the installer for this system.", exception.Message, exception);

    public static bool IsFileError(Exception exception) =>
        exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException;

    private static JobError? StorageFromTool(string stderr, string details)
    {
        if (Has(stderr, "no space left", "disk full", "not enough space", "disk quota exceeded"))
            return StorageMessage(Failure.StorageFull, details);
        if (Has(stderr, "permission denied", "access is denied", "read-only file system", "operation not permitted"))
            return StorageMessage(Failure.StoragePermission, details);
        if (Has(stderr, "used by another process", "sharing violation", "resource busy"))
            return StorageMessage(Failure.StorageBusy, details);
        if (Has(stderr, "unable to open for writing", "unable to write", "error opening output",
                "failed to create directory", "unable to create directory", "could not write header", "file name too long"))
            return StorageMessage(Failure.Storage, details);
        return null;
    }

    private static JobError StorageMessage(Failure kind, string details, Exception? cause = null) => new(kind, kind switch
    {
        Failure.StorageFull => "The destination is out of space. Free up space or choose a different drive and retry.",
        Failure.StoragePermission => "The destination cannot be written to. Choose a folder you can write to, such as Downloads, and retry.",
        Failure.StorageMissing => "The destination folder is unavailable. Reconnect the drive or choose a different local folder.",
        Failure.StorageBusy => "The destination is in use. Close the file in other apps or choose a different output name.",
        _ => "The output could not be saved. Check the destination and choose a different local folder or a shorter filename."
    }, details, cause);

    public static string ToolDetails(string tool, int exit, string stderr) =>
        string.IsNullOrWhiteSpace(stderr) ? $"{tool} exited with code {exit}." :
        $"{tool} exited with code {exit}.{Environment.NewLine}{stderr.TrimEnd()}";

    private static bool Has(string value, params string[] terms) =>
        terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));
}

// Used by one redirected stream. Keep the newest diagnostic text even if the tool emits a long log.
internal sealed class ToolDiagnostics
{
    private const int Limit = 8192;
    private readonly System.Text.StringBuilder _text = new();

    public void Append(string line)
    {
        if (line.Length > Limit)
            line = line[^Limit..];
        _text.AppendLine(line);
        if (_text.Length > Limit)
            _text.Remove(0, _text.Length - Limit);
    }

    public override string ToString() => _text.ToString();
}
