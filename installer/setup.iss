; ============================================================================
;  setup.iss — Inno Setup script for The Council (Windows desktop installer)
;
;  Compiles into TheCouncil-<version>-setup.exe, which the release workflow then
;  zips into TheCouncil-<version>-setup.zip. Installs the published output under
;  Program Files with Start Menu + optional desktop shortcuts and an uninstaller.
;
;  Invoked by .github/workflows/release.yml as:
;    ISCC.exe installer/setup.iss /DMyAppVersion=1.0.0 /DMyAppName=TheCouncil /DPublishDir=publish
; ============================================================================

; ----- Defines (overridable from the command line via /D) -------------------
#ifndef MyAppName
  #define MyAppName "TheCouncil"
#endif
#ifndef MyAppVersion
  #define MyAppVersion "0.0.0"
#endif
#ifndef PublishDir
  #define PublishDir "..\publish"
#endif

#define MyAppPublisher "Ilya Fainberg"
#define MyAppURL "https://github.com/ilyafainberg/the-council"
#define MyAppExeName "TheCouncil.exe"

[Setup]
; Stable AppId — generated once, never change it or upgrades break.
AppId={{5169BE6F-3230-4D27-913F-F32C0CDFF900}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
LicenseFile=..\LICENSE
OutputDir=Output
OutputBaseFilename={#MyAppName}-{#MyAppVersion}-setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
SetupIconFile=..\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Self-contained publish output — bundles the .NET runtime, no prereqs.
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; The marker is written by [Code] below, so remove it on uninstall.
Type: files; Name: "{app}\installed.marker"

[Code]
// Drop an "installed.marker" next to the app so the auto-updater knows this is an
// installer build (and should fetch the *-setup.zip, not the portable zip).
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
    SaveStringToFile(ExpandConstant('{app}\installed.marker'), 'installer', False);
end;

