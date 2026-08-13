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
- Bundled Windows x64 tools in release packages (pinned and hash-verified)
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

The release workflow fetches pinned, hash-verified Windows x64 binaries and publishes a ZIP containing:

- One self-contained `RaminDownloader.exe`
- One self-contained `RaminYtDlpControl.exe` companion control-center GUI
- `tools/win-x64/yt-dlp.exe`
- `tools/win-x64/ffmpeg.exe`
- `tools/win-x64/ffprobe.exe`
- `tools/win-x64/deno.exe`
- third-party notices and checksums

Both applications are published as compressed single-file .NET executables. The
yt-dlp, FFmpeg, FFprobe, and Deno executables remain separate because the app
must launch them as external programs.

The RaminDownloader logo is embedded in the application resources and is also
used as the Windows executable icon; it is not required as a separate package
file.

The tools are release assets rather than normal Git history to keep the repository manageable. Never add browser profiles, cookies, passwords, or tokens to the repository or release.

## Limitations and legal use

Availability depends on the target website and yt-dlp extractor. DRM-protected services are not supported. Use the application only for content you are authorized to download and in compliance with applicable law, copyright, and website terms.

## License

The application source is licensed under the MIT License. The bundled third-party programs retain their own licenses; see `THIRD-PARTY-NOTICES.md`.

## Security

See `SECURITY.md` for reporting instructions and the cookie-handling policy.
