[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\dist')
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$publish = Join-Path $root 'dist\publish'
$controlPublish = Join-Path $root 'dist\control-publish'
$tools = Join-Path $root 'tools\win-x64'
$zip = Join-Path $OutputDirectory 'RaminDownloader-win-x64.zip'

if (Test-Path $publish) { Remove-Item $publish -Recurse -Force }
if (Test-Path $controlPublish) { Remove-Item $controlPublish -Recurse -Force }
if (Test-Path $OutputDirectory) { Remove-Item $OutputDirectory -Recurse -Force }
if (Test-Path $tools) { Remove-Item $tools -Recurse -Force }
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $tools -Force | Out-Null

& dotnet publish (Join-Path $root 'src\RaminDownloader\RaminDownloader.csproj') -c $Configuration -r win-x64 --self-contained true -o $publish
if ($LASTEXITCODE -ne 0) { throw 'RaminDownloader publish failed' }
& dotnet publish (Join-Path $root 'src\RaminYtDlpControl\RaminYtDlpControl.csproj') -c $Configuration -r win-x64 --self-contained true -o $controlPublish
if ($LASTEXITCODE -ne 0) { throw 'RaminYtDlpControl publish failed' }

& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'fetch-tools.ps1') -Destination $tools
if ($LASTEXITCODE -ne 0) { throw 'tool download failed' }

$packageRoot = Join-Path $OutputDirectory 'RaminDownloader'
$packageTools = Join-Path $packageRoot 'tools\win-x64'
$packageAssets = Join-Path $packageRoot 'Assets'
New-Item -ItemType Directory -Path $packageTools -Force | Out-Null
New-Item -ItemType Directory -Path $packageAssets -Force | Out-Null
Copy-Item (Join-Path $publish 'RaminDownloader.exe') $packageRoot -Force
Copy-Item (Join-Path $controlPublish 'RaminYtDlpControl.exe') $packageRoot -Force
if (-not (Test-Path (Join-Path $packageRoot 'RaminDownloader.exe'))) { throw 'RaminDownloader.exe was not produced.' }
if (-not (Test-Path (Join-Path $packageRoot 'RaminYtDlpControl.exe'))) { throw 'RaminYtDlpControl.exe was not produced.' }
Copy-Item (Join-Path $tools '*.exe') $packageTools -Force
Copy-Item (Join-Path $root 'src\RaminDownloader\Assets\RaminDownloader.ico') $packageAssets -Force
Copy-Item (Join-Path $root 'src\RaminDownloader\Assets\ramindownloader-logo.jpg') $packageAssets -Force
Copy-Item (Join-Path $root 'README.md') $packageRoot -Force
Copy-Item (Join-Path $root 'LICENSE') $packageRoot -Force
Copy-Item (Join-Path $root 'SECURITY.md') $packageRoot -Force
Copy-Item (Join-Path $root 'THIRD-PARTY-NOTICES.md') $packageRoot -Force

Get-ChildItem $packageRoot -Filter *.exe | Get-FileHash -Algorithm SHA256 | ForEach-Object {
    "{0}  {1}" -f $_.Hash.ToLowerInvariant(), $_.Name
} | Set-Content (Join-Path $packageRoot 'SHA256SUMS.txt')
Get-ChildItem $packageTools -Filter *.exe | Get-FileHash -Algorithm SHA256 | ForEach-Object {
    "{0}  tools/win-x64/{1}" -f $_.Hash.ToLowerInvariant(), $_.Name
} | Add-Content (Join-Path $packageRoot 'SHA256SUMS.txt')

# Compress-Archive uses Deflate at its highest supported level. The two .NET
# applications are already compressed single-file executables.
Compress-Archive -Path $packageRoot -DestinationPath $zip -CompressionLevel Optimal
Get-FileHash $zip -Algorithm SHA256 | Format-List
Write-Host "Created $zip"

Remove-Item $publish -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item $controlPublish -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item (Join-Path $root 'dist\RaminDownloader') -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item $tools -Recurse -Force -ErrorAction SilentlyContinue
