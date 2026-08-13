namespace RaminDownloader.Models;

public enum DownloadType
{
    Video,
    AudioMp3
}

public enum DownloadQuality
{
    Highest,
    Medium,
    Lowest
}

public sealed record DownloadOptions(
    Uri Url,
    DownloadType Type,
    DownloadQuality Quality,
    bool UseFirefoxCookies)
{
    public static bool TryCreate(
        string? rawUrl,
        DownloadType type,
        DownloadQuality quality,
        bool useFirefoxCookies,
        out DownloadOptions? options)
    {
        options = null;
        if (string.IsNullOrWhiteSpace(rawUrl))
        {
            return false;
        }

        if (!Uri.TryCreate(rawUrl.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Scheme is not ("http" or "https") || string.IsNullOrWhiteSpace(uri.Host))
        {
            return false;
        }

        options = new DownloadOptions(uri, type, quality, useFirefoxCookies);
        return true;
    }
}
