; Inno Setup 6.3+ script for GlassLedge.
; Build it with build\build-installer.ps1, which publishes the app first and passes
; AppVersion and PublishDir. Defaults below allow compiling it straight from the Inno Setup IDE.

#define AppName "GlassLedge"
#define AppPublisher "dah007"
#define AppURL "https://github.com/dah007/glass-ledge"
#define AppExeName "GlassLedge.exe"

#ifndef AppVersion
  #define AppVersion "0.7.0"
#endif

#ifndef PublishDir
  #define PublishDir "..\artifacts\publish\win-x64"
#endif

[Setup]
; AppId identifies the app for upgrades and uninstall. Never change it.
AppId={{B1E966D6-A193-431F-9421-07D66A71E5B3}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}/issues
AppUpdatesURL={#AppURL}/releases
VersionInfoVersion={#AppVersion}

; Per-user install, no admin prompt. {autopf} resolves to %LOCALAPPDATA%\Programs.
PrivilegesRequired=lowest
DefaultDirName={autopf}\{#AppName}
DisableProgramGroupPage=yes
DisableDirPage=auto

; Windows 11 22H2 or later, 64-bit.
MinVersion=10.0.22621
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

OutputDir=..\artifacts\installer
OutputBaseFilename=GlassLedge-Setup-{#AppVersion}
SetupIconFile=..\src\GlassLedge.App\Assets\app.ico
WizardImageFile=wizard-164x314.bmp,wizard-328x628.bmp
WizardSmallImageFile=wizard-small-55.bmp,wizard-small-110.bmp
UninstallDisplayIcon={app}\{#AppExeName}
UninstallDisplayName={#AppName}
WizardStyle=modern
Compression=lzma2/max
SolidCompression=yes

; Close a running copy before upgrading, and start it again afterwards.
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
; Checked by default.
Name: "startup"; Description: "Start {#AppName} when I sign in to Windows"; GroupDescription: "Startup:"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[InstallDelete]
; Leftovers from before the app was renamed (it was called Floating Window Organizer).
Type: files; Name: "{app}\FloatingWindowOrganizer.*"
Type: files; Name: "{autoprograms}\Floating Window Organizer.lnk"

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"

[Registry]
; The old name's startup entry would point at a file that no longer exists, so remove it.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: none; ValueName: "FloatingWindowOrganizer"; Flags: deletevalue
; Per-user "Run" entry; also listed (and switchable) in Settings > Apps > Startup.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "GlassLedge"; ValueData: """{app}\{#AppExeName}"""; Flags: uninsdeletevalue; Tasks: startup

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; The widgets have no window to close from, so stop the app before removing its files.
Filename: "{sys}\taskkill.exe"; Parameters: "/im {#AppExeName} /f"; Flags: runhidden; RunOnceId: "StopApp"

; Widget settings in %APPDATA%\GlassLedge are kept on uninstall so a reinstall restores them.

[Code]
// An old (pre-rename) copy keeps running under its old file name, which the installer would not otherwise close.
procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
begin
  if CurStep = ssInstall then
    Exec(ExpandConstant('{sys}\taskkill.exe'), '/im FloatingWindowOrganizer.exe /f', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;
