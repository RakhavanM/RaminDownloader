# Ramin Downloader 1.1 desktop builds

The Avalonia app combines a yt-dlp downloader and three tools for local files:
resize to a maximum height, compress to MP4 at one of three quality levels,
and convert audio or video. Compression uses a quality target, so the final
size depends on the source. Each window runs one job at a time.

When a job fails, the status area shows what to do next. Expand **Technical
details** to read or copy the original diagnostic when one is available.

Firefox cookies are opt-in for a download. Leave the profile blank for yt-dlp
autodetection, or choose the Firefox profile directory containing
`cookies.sqlite`. This is useful when Firefox was installed with Snap or
Flatpak. The tools run locally, and the app does not save the cookie contents.
Firefox is needed only for using a signed-in Firefox session.

## Installers

The [desktop workflow](../../.github/workflows/build-desktop.yml) builds these
x64 packages with .NET, yt-dlp (including EJS), FFmpeg, FFprobe, and Deno:

- Windows: `RaminDownloader-Setup-win-x64.exe`, a per-user installer.
- Ubuntu/Debian: `ramin-downloader_1.1.0_amd64.deb`. Install with
  `sudo apt install ./ramin-downloader_1.1.0_amd64.deb`. The package uses system
  desktop libraries and adds Ramin Downloader to the application menu.

The workflow stores the installers and checksums as build artifacts. The
Windows installer is unsigned. Local file tools work without an internet
connection. Downloads require network access to the source site. Tool versions
are pinned in `tools.json`; updating the bundle currently means building a
new installer after updating the pins.

## Build locally

Install the .NET 10 SDK, Python 3.12, and curl on the build machine. Publish and
stage tools with one of the RIDs `linux-x64` or `win-x64`:

```sh
dotnet publish src/RaminDownloader.Desktop -c Release -r linux-x64 --self-contained true -o artifacts/app
python3 packaging/desktop/bundle_tools.py linux-x64 artifacts/app/tools
bash packaging/desktop/build-deb.sh artifacts/app dist/ramin-downloader_1.1.0_amd64.deb
```

For Windows, publish and stage with `win-x64`, then compile `setup.iss` using
Inno Setup 6 with `PublishDir` and `DistDir` set to absolute paths. The workflow
contains the full invocation and performs a silent installer check.

See [NOTICES.md](NOTICES.md) before redistributing binaries.
