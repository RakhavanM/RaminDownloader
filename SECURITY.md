# Security, privacy, and responsible disclosure

Ramin Downloader is a local desktop app for Windows and Linux. It runs yt-dlp,
FFmpeg, FFprobe, and Deno on the user's device. It has no app account, media
server, analytics, or telemetry.

Firefox cookie use is optional. When enabled, yt-dlp reads cookies locally and
uses them in requests to the target website. The app does not ask for a
password, store cookie contents, or upload a browser profile to a Ramin
Downloader service. Treat Firefox cookies and profiles as credentials.

The installers include pinned versions of the runtime and media tools. The
build checks the tools against the SHA-256 values in the [bundled tool
manifest](packaging/desktop/tools.json). Download installers from [GitHub
Releases](https://github.com/RakhavanM/RaminDownloader/releases) and compare the
downloaded file with the matching release's `SHA256SUMS.txt`. The Windows
installer is currently unsigned. See the [third-party
notices](packaging/desktop/NOTICES.md) for the bundled components.

Technical diagnostics are displayed locally when you expand **Technical
details**. Review any text you copy from that panel before sharing it, as it
can contain URLs and local file paths.

## Reporting a security issue

Open a private report through the repository's security contact or contact the
repository owner through a trusted GitHub channel. Include the affected app and
OS versions, the impact, and steps to reproduce. Remove private URLs, paths,
passwords, access tokens, and cookies from any logs. Please do not post sensitive
details or exploit code in a public issue, or attach browser profiles, cookie
databases, or private keys.
