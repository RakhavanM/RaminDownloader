# Ramin Downloader

Ramin Downloader is a desktop app for Windows and Linux. Download video or
audio with yt-dlp, or resize, compress, and convert local media with FFmpeg.

## Download and install

Download the installer for your system from the [latest release](https://github.com/RakhavanM/RaminDownloader/releases/latest).
The current stable version is [1.1.0](https://github.com/RakhavanM/RaminDownloader/releases/tag/v1.1.0).

| Platform | Installer |
| --- | --- |
| Windows x64 | [RaminDownloader-Setup-win-x64.exe](https://github.com/RakhavanM/RaminDownloader/releases/download/v1.1.0/RaminDownloader-Setup-win-x64.exe) |
| Ubuntu / Debian x64 | [ramin-downloader_1.1.0_amd64.deb](https://github.com/RakhavanM/RaminDownloader/releases/download/v1.1.0/ramin-downloader_1.1.0_amd64.deb) |

**Windows.** Run the setup file and open **Ramin Downloader** from the Start
menu. The per-user installer is unsigned, so Windows may show a SmartScreen
warning. Verify it against the [release checksums](https://github.com/RakhavanM/RaminDownloader/releases/download/v1.1.0/SHA256SUMS.txt).

**Ubuntu / Debian.** From the folder containing the downloaded file, run:

```sh
sudo apt install ./ramin-downloader_1.1.0_amd64.deb
```

Open **Ramin Downloader** from the application menu. Both installers include the
.NET runtime, yt-dlp, FFmpeg, FFprobe, and Deno. The local file tools work offline
after installation.

## What you can do

- **Download** video or MP3 from a link and choose the quality and output folder.
- **Use Firefox sign-in** when a site requires it. Sign in on the target site
  in Firefox and enable **Use my Firefox sign-in** in the app. Leave the
  profile field blank for automatic detection, or select the profile folder
  containing `cookies.sqlite`. Firefox is only needed for this option.
- **Resize** video to a maximum height while preserving its proportions.
- **Compress** to MP4 with a choice of three quality levels. The final file
  size depends on the source.
- **Convert** local files to MP4, MKV, or WebM for video and MP3, M4A, Opus, WAV,
  or FLAC for audio.
- **Troubleshoot** with a message that suggests what to try. Open **Technical
  details** to inspect or copy the diagnostic when available.

Media processing and Firefox cookie access happen on your device. If enabled,
yt-dlp uses the cookies to make requests to the target website. Use the
downloader for media you are authorized to save. See the [security and privacy
policy](SECURITY.md) for details.

## Build from source

The desktop app uses Avalonia and .NET 10. Open `RaminDownloader.sln` or follow
the [desktop build guide](packaging/desktop/README.md) for packaging, bundled
tool versions, and testing. See the [third-party notices](packaging/desktop/NOTICES.md)
for licensing and source links.

If you are moving from the [legacy Windows ZIP edition](https://github.com/RakhavanM/RaminDownloader/releases/tag/v0.6.2),
download and run the new installer directly.
