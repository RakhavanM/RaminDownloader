using System.Globalization;
using System.Text.RegularExpressions;

namespace RaminDownloader.Services;

public sealed record DownloadProgress(double? Percent, string Message);

public static partial class ProgressParser
{
    [GeneratedRegex(@"(?<percent>\d+(?:\.\d+)?)%", RegexOptions.CultureInvariant)]
    private static partial Regex PercentRegex();

    public static DownloadProgress? Parse(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        var match = PercentRegex().Match(line);
        double? percent = null;
        if (match.Success && double.TryParse(
                match.Groups["percent"].Value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var value))
        {
            percent = Math.Clamp(value, 0, 100);
        }

        return new DownloadProgress(percent, line.Trim());
    }
}
