# RaminDownloader

RaminDownloader is a Windows x64 desktop application for downloading web video or audio that you are authorized to save. It provides two local graphical interfaces around [yt-dlp](https://github.com/yt-dlp/yt-dlp), [FFmpeg](https://ffmpeg.org/), [FFprobe](https://ffmpeg.org/), and [Deno](https://deno.com/).

- **RaminDownloader.exe**: a simple downloader for common video and MP3 downloads.
- **RaminYtDlpControl.exe**: an advanced, tabbed graphical control center for yt-dlp.

The project does not provide access to DRM-protected content and does not bypass website restrictions. Use it only for content you are allowed to download and in accordance with the relevant website terms and applicable law.

## Download and installation

Use the official public repository and its Releases page only:

- Repository: <https://github.com/RakhavanM/RaminDownloader>
- Releases: <https://github.com/RakhavanM/RaminDownloader/releases/latest>

1. Download `RaminDownloader-win-x64.zip` and the matching `SHA256SUMS.txt` from the same Release.
2. Verify the ZIP before extracting it. In PowerShell:

   ```powershell
   (Get-FileHash .\RaminDownloader-win-x64.zip -Algorithm SHA256).Hash.ToLower()
   ```

   Compare the result with the value for that ZIP in `SHA256SUMS.txt`.
3. Extract the **complete ZIP** to a normal user-writable folder, for example:

   ```text
   C:\Users\<your-name>\Downloads\RaminDownloader\
   ```

4. Run `RaminDownloader.exe` or `RaminYtDlpControl.exe` from the extracted folder. Do not run either program directly from inside the ZIP.
5. On the first run, allow the application to download its missing media tools. An Internet connection is required for this step.

The application is self-contained; installing the .NET Desktop Runtime is not required. The release is Windows x64 only. Installing it under `Program Files` is not recommended because the application needs a writable `Assets\tools` directory for first-run dependency installation and updates.

## What is inside the ZIP?

The release ZIP intentionally does **not** include the large third-party tool binaries. It contains:

```text
RaminDownloader.exe
RaminYtDlpControl.exe
Assets\tools-manifest.json
README.md
SECURITY.md
THIRD-PARTY-NOTICES.md
LICENSE
SHA256SUMS.txt
```

The tools are installed on demand in the extracted application folder:

```text
Assets\tools\
├── yt-dlp.exe
├── ffmpeg.exe
├── ffprobe.exe
└── deno.exe
```

At startup, the application first looks for all four tools in `Assets\tools` and then checks the Windows `PATH`. If the complete set is not available, it downloads the official Windows x64 assets described in `Assets\tools-manifest.json` and verifies their SHA-256 checksums before activating them.

## RaminDownloader: simple mode

The main application is designed for a quick, low-complexity workflow:

- URL input with a **Paste** button.
- **Video** or **Audio (MP3)** selection.
- **Highest**, **Medium**, or **Lowest** quality selection.
- MP4-compatible video output.
- Progress bar and the latest yt-dlp output line.
- Cancel button.
- Completion dialog showing the download folder.
- Automatic form reset after the completion dialog is closed.
- Firefox cookie access through yt-dlp when the site requires a logged-in session.
- **UPDATE** button for dependency updates and application-release checks.

Downloads from the simple mode are saved under:

```text
%USERPROFILE%\Downloads\RaminDownloader\
```

## RaminYtDlpControl: advanced mode

`RaminYtDlpControl.exe` is a more flexible GUI for users who need direct control over yt-dlp. The settings are divided into tabs so the interface remains manageable.

### General

- One or more URLs, one per line.
- Output folder.
- Output filename template.
- Use Firefox cookies automatically.
- Disable playlist downloads.
- Download archive file to avoid downloading the same item twice.

### Format

- Best video plus best audio.
- Best MP4-compatible format.
- Audio-only selection.
- Custom yt-dlp format expression.
- MP4, MKV, or WebM merge format.
- Extract audio as MP3, M4A, Opus, FLAC, or WAV.
- Audio quality selection.
- Embed metadata and thumbnail.

### Post-processing

- Write thumbnails.
- Keep the original video after audio extraction.
- Remux video when possible.

### Subtitles & Metadata

- Write subtitles.
- Write automatically generated subtitles.
- Select subtitle languages.
- Select subtitle format such as Best, SRT, VTT, or ASS.
- Write the video description.
- Write an information JSON file.

### Playlist

- Playlist start and end positions.
- Reverse playlist order.
- Random playlist order.

### Network

- Proxy.
- Rate limit.
- Download retries.
- Fragment retries.

### Advanced

- Verbose yt-dlp output.
- Additional yt-dlp arguments.
- Generated-command preview before execution.

The **Run** button opens a visible PowerShell terminal and executes the generated yt-dlp command. The command is passed with explicit argument quoting rather than by concatenating an untrusted shell command. If yt-dlp finishes successfully, the terminal closes. If it returns an error, the terminal stays open so the error can be read. The **Cancel** button can terminate the running process tree.

Review the command preview before using custom arguments. The additional-arguments field is intentionally powerful and can change yt-dlp's behavior; only enter options you understand.

## Firefox cookies and privacy

Firefox is used only as a local browser-cookie source when the user enables the cookie option. The normal workflow is:

1. Install Firefox and sign in to the target website in Firefox.
2. Complete any CAPTCHA or verification in Firefox.
3. Close Firefox if its profile database is locked.
4. Start the download.

The application passes `--cookies-from-browser firefox` to the local yt-dlp process. It does not ask for a password, export a cookies file, copy a browser profile, or send cookie contents to a RaminDownloader server. Cookies are still active session credentials: yt-dlp uses them in requests to the target website, and the target website can process those requests under its own privacy policy.

The application has no account system, advertising SDK, analytics service, or application telemetry service. It does not upload downloaded URLs, browser profiles, passwords, or cookies to an application backend. The application does contact external services for their stated purposes:

- The target website, through yt-dlp, to retrieve the requested media.
- GitHub and the upstream projects, to download and verify missing tools.
- GitHub Releases, to check for and download an application update when the user presses **UPDATE**.

The advanced GUI creates a temporary PowerShell script containing the generated command and removes it on completion on a best-effort basis. The script contains the URL and selected options, not the contents of Firefox cookies.

## Updates

The **UPDATE** button in either GUI performs two checks:

1. It downloads the latest yt-dlp, FFmpeg, FFprobe, and Deno assets when requested, verifies their checksums, and installs them atomically under `Assets\tools`.
2. It checks the latest public RaminDownloader GitHub Release. If a newer application release exists, it asks for confirmation, downloads the ZIP and its release checksum, verifies the ZIP, and schedules a restart-based replacement.

Application updates require the extracted application folder to be writable. Do not run the updater as Administrator to work around a protected installation; move the application to a user-writable folder instead.

A checksum protects against corruption, an incorrect asset, and many forms of transit tampering. It is not a replacement for a digital signature, so always obtain the manifest, ZIP, and checksum from the official project or upstream release pages.

## Windows Defender SmartScreen warning

Some users may see:

> Windows protected your PC
>
> Microsoft Defender SmartScreen prevented an unrecognized app from starting.
>
> App: RaminDownloader.exe
>
> Publisher: Unknown publisher

This warning is expected for the current public ZIP distribution because the EXE files are **not Authenticode code-signed**. Windows therefore cannot display a verified publisher name, and the new file has little or no SmartScreen download reputation. The warning does **not** mean that Microsoft has identified RaminDownloader as malware. It also does **not** prove that any downloaded program is safe; it means that Windows cannot establish enough publisher or file reputation to approve it automatically.

Microsoft describes SmartScreen reputation as depending on publisher identity, file identity, and download history. New or unsigned applications can show this warning even when they are legitimate. A future code-signing certificate or Microsoft Store distribution may improve the publisher signal, but signing alone does not guarantee that a new release will have no warning.

Before choosing to run a downloaded executable:

1. Confirm that it came from the official GitHub Releases page above.
2. Verify the ZIP SHA-256 value against the matching `SHA256SUMS.txt` file.
3. Extract the complete ZIP and confirm that the EXE is inside that verified folder.
4. If SmartScreen shows **More info**, verify the app name and source. Only then, and only if the hash and source are trusted, use **Run anyway** if you choose to continue.
5. If **More info** or **Run anyway** is not available, do not disable Defender or bypass a company policy. Close the dialog and investigate the source or ask your administrator.

Do not download a replacement EXE from a random mirror, do not disable Microsoft Defender, and do not run the program as Administrator merely to dismiss SmartScreen.

## Security boundaries and limitations

- Windows x64 only.
- No DRM circumvention.
- No password collection.
- No server-side cookie storage.
- No guarantee that a website's extractor will continue to work; yt-dlp and website changes can affect availability.
- The app cannot make claims about the privacy practices of the target website, Firefox, GitHub, yt-dlp, FFmpeg, Deno, or any custom yt-dlp argument selected by the user.
- Keep Firefox and Windows updated, and treat browser cookies as credentials.

## Development and licensing

The GUI targets `net8.0-windows` and is published self-contained for `win-x64`. The test project targets `net8.0` so argument construction and progress parsing can be tested separately.

```powershell
dotnet restore
dotnet test
dotnet build -c Release
```

The application source is licensed under MIT. Third-party tools retain their own licenses; see [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md). For security reporting instructions, see [SECURITY.md](SECURITY.md).

## Legal use

Use RaminDownloader only for content you own or are authorized to download. Respect copyright, access controls, website terms, and applicable law.

## Links

- Repository: <https://github.com/RakhavanM/RaminDownloader>
- Releases: <https://github.com/RakhavanM/RaminDownloader/releases>
- yt-dlp: <https://github.com/yt-dlp/yt-dlp>
- FFmpeg: <https://ffmpeg.org/>
- FFmpeg Windows builds used by the manifest: <https://github.com/yt-dlp/FFmpeg-Builds>
- Deno: <https://github.com/denoland/deno>
- Microsoft SmartScreen reputation guidance: <https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/smartscreen-reputation>
