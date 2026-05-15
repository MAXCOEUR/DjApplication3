#define MyAppName "DjApplication 3"
#define MyAppVersion "2.0.4"
#define MyAppPublisher "MaxenceCOEUR"
#define MyAppExeName "DjApplication3.WinUI.exe"
#define RepoRoot AddBackslash(SourcePath) + "..\"
#define PublishDir RepoRoot + "DjApplication3.WinUI\bin\Release\net8.0-windows10.0.19041.0\win-x64\publish"

#if !FileExists(PublishDir + "\DjApplication3.WinUI.exe") || !FileExists(PublishDir + "\App.xbf") || !FileExists(PublishDir + "\DjApplication3.WinUI.pri")
  #error "Publish incomplet. Lance d'abord SetupInno\build-installer.ps1 pour generer le publish et compiler ce setup."
#endif

[Setup]
AppId={{7E4F6469-7D10-4A58-9B47-06011DA4B637}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\MaxenceCOEUR\DjApplication
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=Output
OutputBaseFilename=DjApplication3Setup
Compression=lzma
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
SetupLogging=yes
UninstallDisplayIcon={app}\{#MyAppExeName}
SetupIconFile={#RepoRoot}DjApplication3.WinUI\Resources\logo.ico

[Languages]
Name: "french"; MessagesFile: "compiler:Languages\French.isl"

[Tasks]
Name: "desktopicon"; Description: "Creer un raccourci sur le bureau"; GroupDescription: "Raccourcis :"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\Resources\logo.ico"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\Resources\logo.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Lancer {#MyAppName}"; Flags: nowait postinstall skipifsilent
