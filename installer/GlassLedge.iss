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
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "GlassLedge"; ValueData: """{app}\{#AppExeName}"""; Flags: uninsdeletevalue; Check: StartWithWindows

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; The widgets have no window to close from, so stop the app before removing its files.
Filename: "{sys}\taskkill.exe"; Parameters: "/im {#AppExeName} /f"; Flags: runhidden; RunOnceId: "StopApp"

; Widget settings in %APPDATA%\GlassLedge are kept on uninstall so a reinstall restores them.

[Code]
// ---- Look: follows the Preferences window (Segoe UI Variable, semibold 14 px titles, 12.5 px secondary
// text, a rounded-looking row with an icon tile). Colours are the light brand palette, as Inno BGR values.
const
  ColorText = $1E1C1C;          // Graphite
  ColorSecondary = $5C5552;     // brand secondary text
  ColorFocus = $00538A;         // deep amber
  ColorCard = $F7F5F4;          // faint tile on the white page
  ColorTile = $E9E5E2;          // icon tile

var
  StartCheck: TNewCheckBox;

// "Start with Windows" row on its own page. Silent installs can pass /StartWithWindows=0 to leave it off.
function StartWithWindows: Boolean;
begin
  Result := (StartCheck = nil) or StartCheck.Checked;
end;

procedure UseFont(Control: TNewStaticText; const Name: String; Size: Integer; Color: TColor);
begin
  Control.Font.Name := Name;
  Control.Font.Size := Size;
  Control.Font.Style := [];
  Control.Font.Color := Color;
end;

procedure InitializeWizard;
var
  Page: TWizardPage;
  Card, Tile: TPanel;
  Glyph, Detail: TNewStaticText;
begin
  // Typography. Only names, styles and colours change; sizes stay so Inno's own layout still fits.
  WizardForm.Font.Name := 'Segoe UI Variable Text';
  WizardForm.PageNameLabel.Font.Name := 'Segoe UI Semibold';
  WizardForm.PageNameLabel.Font.Style := [];
  WizardForm.PageNameLabel.Font.Color := ColorText;
  WizardForm.PageDescriptionLabel.Font.Color := ColorSecondary;
  WizardForm.WelcomeLabel1.Font.Name := 'Segoe UI Semibold';
  WizardForm.WelcomeLabel1.Font.Style := [];
  WizardForm.FinishedHeadingLabel.Font.Name := 'Segoe UI Semibold';
  WizardForm.FinishedHeadingLabel.Font.Style := [];

  // Startup option, laid out like a Preferences row: icon tile, title, then detail text beneath.
  Page := CreateCustomPage(wpSelectDir, 'Startup', 'Choose whether GlassLedge opens when you sign in.');

  Card := TPanel.Create(Page);
  Card.Parent := Page.Surface;
  Card.SetBounds(0, 0, Page.SurfaceWidth, ScaleY(72));
  Card.BevelOuter := bvNone;
  Card.ParentBackground := False;
  Card.Color := ColorCard;

  Tile := TPanel.Create(Page);
  Tile.Parent := Card;
  Tile.SetBounds(ScaleX(14), ScaleY(18), ScaleX(36), ScaleY(36));
  Tile.BevelOuter := bvNone;
  Tile.ParentBackground := False;
  Tile.Color := ColorTile;

  // The same power icon as Preferences, from the Windows icon font.
  Glyph := TNewStaticText.Create(Page);
  Glyph.Parent := Tile;
  Glyph.Caption := #$E7E8;
  UseFont(Glyph, 'Segoe Fluent Icons', 12, ColorFocus);
  Glyph.Left := (Tile.Width - Glyph.Width) div 2;
  Glyph.Top := (Tile.Height - Glyph.Height) div 2;

  StartCheck := TNewCheckBox.Create(Page);
  StartCheck.Parent := Card;
  StartCheck.SetBounds(ScaleX(64), ScaleY(12), Card.Width - ScaleX(78), ScaleY(22));
  StartCheck.Caption := 'Start with Windows';
  StartCheck.Font.Name := 'Segoe UI Semibold';
  StartCheck.Font.Size := 10;
  StartCheck.Font.Color := ColorText;
  StartCheck.Checked := ExpandConstant('{param:StartWithWindows|1}') <> '0';

  Detail := TNewStaticText.Create(Page);
  Detail.Parent := Card;
  Detail.AutoSize := False;
  Detail.WordWrap := True;
  Detail.SetBounds(ScaleX(64) + ScaleX(20), ScaleY(36), Card.Width - ScaleX(64) - ScaleX(34), ScaleY(32));
  Detail.Caption := 'Open GlassLedge in the tray when you sign in to Windows.';
  UseFont(Detail, 'Segoe UI Variable Text', 9, ColorSecondary);
end;

// An old (pre-rename) copy keeps running under its old file name, which the installer would not otherwise close.
procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
begin
  if CurStep = ssInstall then
    Exec(ExpandConstant('{sys}\taskkill.exe'), '/im FloatingWindowOrganizer.exe /f', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;
