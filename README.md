# RaminDownloader

A private Windows x64 WPF desktop GUI for downloading permitted web video or MP3 audio through [yt-dlp](https://github.com/yt-dlp/yt-dlp), [FFmpeg](https://ffmpeg.org/), and [Deno](https://deno.com/).

## Features

- URL input and Download button
- Video or Audio (MP3) selection
- Highest, Medium, or Lowest quality selection
- Progress bar, status, and live latest yt-dlp output
- Completion dialog followed by an automatic form reset
- Clipboard Paste button for the URL
- Cancellation support
- MP4 video output
- Downloads Windows x64 tools on first run and verifies their hashes
- Downloads missing yt-dlp, FFmpeg, FFprobe, and Deno on first launch
- UPDATE button for dependency and application updates
- Firefox cookies read locally through yt-dlp

## Firefox cookie note

Install Firefox and sign in to the website you want to download from. Complete any CAPTCHA or verification in Firefox, then close Firefox before starting the download if its profile is locked. The application passes `--cookies-from-browser firefox` to yt-dlp; it does not upload, export, or store cookies.

Firefox is recommended on Windows. Modern Chromium browsers can use app-bound cookie encryption that prevents external tools from reading some cookies. RaminDownloader does not ask for passwords and does not run as Administrator.

## Development

The GUI project targets `net8.0-windows` and is intended to build on Windows or in a Windows GitHub Actions runner. The unit-test project targets `net8.0` so argument construction and progress parsing can be tested on Linux CI as well.

```powershell
dotnet restore
dotnet test
 dotnet build -c Release
```

## Release packaging

The release workflow publishes a ZIP containing:

- One self-contained `RaminDownloader.exe`
- One self-contained `RaminYtDlpControl.exe` companion control-center GUI
- `Assets/tools-manifest.json` for first-run dependency downloads
- third-party notices and checksums

Both applications are published as compressed single-file .NET executables. The
yt-dlp, FFmpeg, FFprobe, and Deno executables remain separate because the app
must launch them as external programs.

The tools are downloaded on first launch into `Assets/tools` and verified with
SHA-256 checksums. The UPDATE button checks the public GitHub release and the
latest upstream dependency downloads. Never add browser profiles, cookies,
passwords, or tokens to the repository or release.

## Limitations and legal use

Availability depends on the target website and yt-dlp extractor. DRM-protected services are not supported. Use the application only for content you are authorized to download and in compliance with applicable law, copyright, and website terms.

## License

The application source is licensed under the MIT License. The bundled third-party programs retain their own licenses; see `THIRD-PARTY-NOTICES.md`.

## Security

See `SECURITY.md` for reporting instructions and the cookie-handling policy.
