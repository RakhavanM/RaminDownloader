[CmdletBinding()]
param(
    [string]$Destination = (Join-Path $PSScriptRoot '..\tools\win-x64')
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$tools = @(
    @{ Name = 'yt-dlp.exe'; Url = 'https://github.com/yt-dlp/yt-dlp/releases/download/2026.07.04/yt-dlp.exe'; Sha256 = '52fe3c26dcf71fbdc85b528589020bb0b8e383155cfa81b64dd447bbe35e24b8' },
    @{ Name = 'ffmpeg-N-126061-g844e10e1a7-win64-gpl.zip'; Url = 'https://github.com/yt-dlp/FFmpeg-Builds/releases/download/autobuild-2026-08-11-18-08/ffmpeg-N-126061-g844e10e1a7-win64-gpl.zip'; Sha256 = 'cdb6000941ef5c3b39701202ae9acad12cb383f81c3309556cd2b035e56d348d' },
    @{ Name = 'deno-x86_64-pc-windows-msvc.zip'; Url = 'https://github.com/denoland/deno/releases/download/v2.9.5/deno-x86_64-pc-windows-msvc.zip'; Sha256 = '171efab55ac6b9881fd53ee4c20f8bf3bb1340ffc618483746909014db12216a' }
)

$staging = Join-Path $env:TEMP ('RaminDownloader-tools-' + [guid]::NewGuid())
New-Item -ItemType Directory -Path $staging -Force | Out-Null
New-Item -ItemType Directory -Path $Destination -Force | Out-Null

try {
    foreach ($tool in $tools) {
        $archive = Join-Path $staging $tool.Name
        Invoke-WebRequest -Uri $tool.Url -OutFile $archive
        $actual = (Get-FileHash -Algorithm SHA256 -LiteralPath $archive).Hash.ToLowerInvariant()
        if ($actual -ne $tool.Sha256) {
            throw "Hash mismatch for $($tool.Name): expected $($tool.Sha256), got $actual"
        }

        if ($tool.Name -eq 'yt-dlp.exe') {
            Copy-Item $archive (Join-Path $Destination 'yt-dlp.exe') -Force
        }
        elseif ($tool.Name.StartsWith('ffmpeg-')) {
            Expand-Archive $archive (Join-Path $staging 'ffmpeg') -Force
            Copy-Item (Get-ChildItem (Join-Path $staging 'ffmpeg') -Filter ffmpeg.exe -Recurse | Select-Object -First 1).FullName (Join-Path $Destination 'ffmpeg.exe') -Force
            Copy-Item (Get-ChildItem (Join-Path $staging 'ffmpeg') -Filter ffprobe.exe -Recurse | Select-Object -First 1).FullName (Join-Path $Destination 'ffprobe.exe') -Force
        }
        else {
            Expand-Archive $archive (Join-Path $staging 'deno') -Force
            Copy-Item (Get-ChildItem (Join-Path $staging 'deno') -Filter deno.exe -Recurse | Select-Object -First 1).FullName (Join-Path $Destination 'deno.exe') -Force
        }
    }

    Get-ChildItem $Destination -Filter *.exe | Get-FileHash -Algorithm SHA256 | ForEach-Object {
        "{0}  {1}" -f $_.Hash.ToLowerInvariant(), $_.Path
    }
}
finally {
    Remove-Item $staging -Recurse -Force -ErrorAction SilentlyContinue
}
