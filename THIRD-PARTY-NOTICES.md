# Third-party notices

RaminDownloader is a local Windows x64 GUI. The release ZIP does not bundle the large third-party executable files. On first run, the application downloads the upstream assets named in `Assets/tools-manifest.json`, verifies their SHA-256 checksums, and stores them locally under `Assets\tools`.

The application does not modify the upstream tools. Their licenses, notices, and source obligations remain with their respective projects.

## yt-dlp

- Project: <https://github.com/yt-dlp/yt-dlp>
- License: Unlicense, with bundled third-party components and notices described by the upstream project.
- Runtime asset: `yt-dlp.exe`, obtained from the official yt-dlp GitHub release URL in `Assets/tools-manifest.json`.

## FFmpeg and FFprobe

- Project: <https://ffmpeg.org/>
- Windows build source: <https://github.com/yt-dlp/FFmpeg-Builds>
- Runtime assets: `ffmpeg.exe` and `ffprobe.exe`, obtained from the official FFmpeg-Builds release URL in `Assets/tools-manifest.json`.
- License: The selected Windows build is distributed under the applicable FFmpeg/GPL notices. Consult the upstream build repository and FFmpeg documentation for the complete license text and source obligations.

## Deno

- Project: <https://deno.com/>
- Source: <https://github.com/denoland/deno>
- Runtime asset: `deno.exe`, obtained from the official Deno GitHub release URL in `Assets/tools-manifest.json`.
- License: MIT, with third-party components and notices described by the upstream project.

## .NET

- Project: <https://dotnet.microsoft.com/>
- The applications are published self-contained using the .NET runtime. Consult Microsoft's .NET licensing and attribution notices for the runtime components.

## Update and checksum scope

The manifest and release ZIP identify the download URLs used by the application. The application retrieves upstream checksum files and refuses to activate a dependency when a checksum is missing or does not match. A checksum is an integrity check, not a digital signature; users should still obtain the application and manifest from the official repository and release pages.

The project does not claim ownership of, affiliation with, or endorsement by yt-dlp, FFmpeg, Deno, Microsoft, or GitHub.
