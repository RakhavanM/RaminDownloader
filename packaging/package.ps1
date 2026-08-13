[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\dist')
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$publish = Join-Path $root 'dist\publish'
$tools = Join-Path $root 'tools\win-x64'
$zip = Join-Path $OutputDirectory 'RaminDownloader-win-x64.zip'

if (Test-Path $publish) { Remove-Item $publish -Recurse -Force }
if (Test-Path $OutputDirectory) { Remove-Item $OutputDirectory -Recurse -Force }
if (Test-Path $tools) { Remove-Item $tools -Recurse -Force }
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $tools -Force | Out-Null

& dotnet publish (Join-Path $root 'src\RaminDownloader\RaminDownloader.csproj') -c $Configuration -r win-x64 --self-contained true -o $publish
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed' }

& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'fetch-tools.ps1') -Destination $tools
if ($LASTEXITCODE -ne 0) { throw 'tool download failed' }

$packageRoot = Join-Path $OutputDirectory 'RaminDownloader'
$packageTools = Join-Path $packageRoot 'tools\win-x64'
New-Item -ItemType Directory -Path $packageTools -Force | Out-Null
Copy-Item (Join-Path $publish '*') $packageRoot -Recurse -Force
Copy-Item (Join-Path $tools '*.exe') $packageTools -Force
Copy-Item (Join-Path $root 'README.md') $packageRoot -Force
Copy-Item (Join-Path $root 'LICENSE') $packageRoot -Force
Copy-Item (Join-Path $root 'SECURITY.md') $packageRoot -Force
Copy-Item (Join-Path $root 'THIRD-PARTY-NOTICES.md') $packageRoot -Force

Get-ChildItem $packageTools -Filter *.exe | Get-FileHash -Algorithm SHA256 | ForEach-Object {
    "{0}  tools/win-x64/{1}" -f $_.Hash.ToLowerInvariant(), $_.Name
} | Set-Content (Join-Path $packageRoot 'SHA256SUMS.txt')

Compress-Archive -Path $packageRoot -DestinationPath $zip -CompressionLevel Optimal
Get-FileHash $zip -Algorithm SHA256 | Format-List
Write-Host "Created $zip"

# Keep generated artifacts outside the source tree after packaging.
Remove-Item $publish -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item (Join-Path $root 'dist\RaminDownloader') -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item $tools -Recurse -Force -ErrorAction SilentlyContinue
# The ZIP remains at $zip for the workflow to upload.
