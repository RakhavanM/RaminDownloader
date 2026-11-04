#define AppName "Ramin Downloader"
#ifndef AppVersion
  #define AppVersion "1.1.0"
#endif
#ifndef PublishDir
  #error PublishDir must point to the self-contained publish directory
#endif
#ifndef DistDir
  #error DistDir must point to the installer output directory
#endif

[Setup]
AppId=RaminDownloader.Desktop
AppName={#AppName}
AppVersion={#AppVersion}
DefaultDirName={localappdata}\Programs\Ramin Downloader
DefaultGroupName={#AppName}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
OutputDir={#DistDir}
OutputBaseFilename=RaminDownloader-Setup-win-x64
Compression=lzma2/fast
SolidCompression=yes
WizardStyle=modern
SetupIconFile=..\..\src\RaminDownloader\Assets\RaminDownloader.ico
UninstallDisplayIcon={app}\RaminDownloader.Desktop.exe

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "NOTICES.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\RaminDownloader.Desktop.exe"

[Run]
Filename: "{app}\RaminDownloader.Desktop.exe"; Description: "Open Ramin Downloader"; Flags: nowait postinstall skipifsilent
