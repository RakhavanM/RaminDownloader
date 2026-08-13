# Security policy

Please report security issues privately to the repository owner rather than opening a public issue.

RaminDownloader is a local desktop application. It does not upload browser cookies, passwords, browser profiles, or downloaded URLs to an application server. Firefox cookies are passed locally to yt-dlp through `--cookies-from-browser firefox`.

Do not run the application as Administrator. Do not share exported cookies files. Treat browser cookies as active session credentials.

The release build pins and verifies the hashes of bundled third-party executables before packaging.