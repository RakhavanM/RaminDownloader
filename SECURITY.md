# Security, privacy, and responsible disclosure

## Scope

RaminDownloader is a local Windows x64 desktop application. It provides a graphical interface for running yt-dlp, FFmpeg, FFprobe, and Deno on the user's own computer. It is not a hosted downloader, account service, cloud storage service, or cookie-collection service.

The project is intended for content the user owns or is authorized to download. It does not support DRM circumvention or bypassing access controls.

## What the application does

The application can:

- Run yt-dlp locally for a requested URL.
- Run FFmpeg and FFprobe locally for media selection, merging, and post-processing.
- Run Deno locally as a JavaScript runtime for yt-dlp where required by an extractor.
- Read Firefox cookies locally through yt-dlp when the user enables Firefox cookie use.
- Download missing official tool releases into the local `Assets\tools` directory.
- Check SHA-256 values before activating downloaded tool files.
- Check the public GitHub Releases API and download a verified application ZIP when the user presses **UPDATE**.

The application does not provide a server that receives the user's media URLs or browser data.

## Privacy commitments and boundaries

The application does **not** intentionally:

- Upload Firefox cookies or browser profiles to RaminDownloader.
- Ask for, collect, or store Firefox passwords.
- Export cookies to a user-visible cookies file.
- Send browser profiles, passwords, downloaded URLs, or application telemetry to a RaminDownloader backend.
- Include an analytics SDK, advertising SDK, or RaminDownloader account system.
- Require Administrator privileges.

Firefox cookies are sensitive, active session credentials. When the option is enabled, yt-dlp reads them locally and uses them in requests to the target website. The target website can observe and process those requests under its own policies. A user must never share an exported cookie file or a browser profile with another person.

The advanced control-center GUI creates a temporary PowerShell script to launch the visible yt-dlp terminal. It contains the selected URL and command options, not the contents of Firefox cookies. The script is deleted when the terminal exits on a best-effort basis. Avoid entering passwords, access tokens, or other secrets in the URL, output template, proxy field, or additional-arguments field.

The application does contact external services for limited, visible purposes:

| Service | Purpose |
|---|---|
| Target website | yt-dlp retrieves the requested media or metadata. |
| GitHub Releases | The app obtains the latest application release when the user presses **UPDATE**. |
| Official upstream release hosts | The app downloads yt-dlp, FFmpeg/FFprobe, and Deno when they are missing or updated. |

The privacy behavior of the target website, Firefox, GitHub, yt-dlp, FFmpeg, Deno, and custom yt-dlp arguments is outside this project's control.

## Dependency-download security

The ZIP does not ship the large third-party binaries. On first run, the app downloads the assets named in `Assets\tools-manifest.json` into `Assets\tools`.

The installer/update flow:

1. Downloads to a temporary staging directory, not directly over the active executable.
2. Uses official upstream HTTPS release URLs listed in the manifest.
3. Downloads the corresponding upstream checksum file.
4. Computes SHA-256 locally.
5. Refuses to activate a file when the checksum is missing or does not match.
6. Extracts archives into temporary directories.
7. Replaces the active tool files only after validation.

A checksum detects corruption, an incorrect file, and many forms of transit tampering. It is not a digital signature and does not replace verifying that the ZIP and manifest came from the official project or upstream release page.

## Application-update security

The **UPDATE** button checks the public GitHub Releases API. For a newer release, the application:

1. Downloads the release ZIP and the matching `SHA256SUMS.txt` file.
2. Verifies the ZIP checksum.
3. Requires the application directory to be writable.
4. Schedules a helper to wait for the current process to close.
5. Extracts and replaces the application files.
6. Starts the selected application again.

The update helper is not an elevation mechanism. If the application is under a protected directory such as `Program Files`, move the complete folder to a user-writable location instead of running the app as Administrator.

## Windows Defender SmartScreen

The current EXE release is not Authenticode code-signed. Windows may therefore show:

> Windows protected your PC
>
> Microsoft Defender SmartScreen prevented an unrecognized app from starting.
>
> Publisher: Unknown publisher

This is an **unrecognized publisher/reputation warning**, not a statement that Microsoft has detected malware in RaminDownloader. A new unsigned file has no verified publisher identity and little or no SmartScreen file reputation. Microsoft notes that new or unsigned applications may show this warning until publisher and file reputation are established.

Users should not treat the warning as proof of safety. Before running the app:

1. Download only from the official GitHub Releases page.
2. Verify the ZIP against the matching `SHA256SUMS.txt` file.
3. Keep the complete extracted folder together.
4. If Windows offers **More info**, confirm the app name and source before deciding whether **Run anyway** is appropriate.
5. Never disable Defender, bypass an organization policy, or use a random mirrored executable to get around SmartScreen.

A future Authenticode certificate or Microsoft Store distribution may improve publisher recognition. Code signing is planned as a distribution improvement; it cannot replace source verification and hash verification.

## User safety recommendations

- Keep Windows, Firefox, and security software updated.
- Do not run the application as Administrator.
- Keep the application in a user-writable folder.
- Do not share Firefox profiles, cookies, passwords, or tokens.
- Close Firefox if its profile database is locked and yt-dlp cannot read cookies.
- Review the generated command before running custom yt-dlp arguments.
- Verify release checksums after downloading a new ZIP.
- Use only content and websites for which you have permission.

## Reporting a security issue

Please do not publish sensitive details, cookies, URLs containing tokens, or exploit code in a public issue.

Open a private report through the repository's security contact or contact the repository owner through a trusted GitHub channel. Include:

- A short description and impact.
- The affected release version and Windows version.
- Reproduction steps that do not contain credentials or private URLs.
- Relevant logs with cookies, passwords, access tokens, and personal paths removed.
- A safe contact method for follow-up.

Do not attach browser profiles, cookie databases, exported cookie files, passwords, or private keys.

## Third-party components

The application relies on external projects that have their own security policies and release processes:

- yt-dlp: <https://github.com/yt-dlp/yt-dlp>
- FFmpeg: <https://ffmpeg.org/>
- FFmpeg Windows builds: <https://github.com/yt-dlp/FFmpeg-Builds>
- Deno: <https://github.com/denoland/deno>
- .NET: <https://dotnet.microsoft.com/>

See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) for license and attribution information.
