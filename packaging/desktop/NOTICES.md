# Bundled tools

`tools/versions.json` identifies the exact upstream assets, download locations, and SHA-256 hashes.

- **yt-dlp**: [source, release files and licensing](https://github.com/yt-dlp/yt-dlp/tree/2026.10.26).
  The standalone executables include third-party dependencies, including the EJS scripts.
- **FFmpeg and FFprobe**: the bundled static GPL build, including x264, is from
  [yt-dlp/FFmpeg-Builds](https://github.com/yt-dlp/FFmpeg-Builds/releases/tag/autobuild-2026-11-04-20-55).
  See the [FFmpeg source revision](https://github.com/FFmpeg/FFmpeg/tree/5048d707a3),
  [build project and recipes](https://github.com/yt-dlp/FFmpeg-Builds), and
  [license information](https://ffmpeg.org/legal.html). Public distribution of these
  packages requires making the complete corresponding source and license texts
  for the exact redistributed builds available.
- **Deno**: [source and licensing](https://github.com/denoland/deno/tree/v2.9.8).
- **.NET**: [runtime licenses and notices](https://github.com/dotnet/runtime).
- **Avalonia**: [source and license](https://github.com/AvaloniaUI/Avalonia).

Ramin Downloader invokes the media tools as separate processes.
