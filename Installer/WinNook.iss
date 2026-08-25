; WinNook installer
; Build with:  "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" Installer\WinNook.iss
;
; Expects a self-contained publish to have run first:
;   dotnet publish "Project Nook.csproj" -c Release -r win-x64 --self-contained true -p:Platform=x64

#define AppName        "WinNook"
#define AppVersion     "2.0.0"
#define AppPublisher   "Midhlaj AM"
#define AppURL         "https://github.com/MidhlajAm"
#define AppExe         "WinNook.exe"
#define PublishDir     "..\bin\Release\net8.0-windows10.0.19041.0\win-x64\publish"

[Setup]
; A stable GUID identifies this product across upgrades. Never change it, or an update will
; install alongside the old copy instead of replacing it.
AppId={{8F3C1A62-5B47-4E9D-9C21-6A7E2D40B5F1}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}

; Per-user by default, so installing needs no UAC prompt. WinNook only ever writes to HKCU and
; the user's AppData, so it has no reason to demand administrator rights. Users who want an
; all-users install can still elevate from the dialog.
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#AppExe}
UninstallDisplayName={#AppName}

OutputDir=Output
OutputBaseFilename={#AppName}-{#AppVersion}-Setup
SetupIconFile=..\Assets\winnook.ico

; A self-contained .NET app is mostly compressible framework code, so maximum LZMA2 pays for
; itself several times over here.
Compression=lzma2/max
SolidCompression=yes
InternalCompressLevel=max

WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763

; Matches the mutex in App.OnStartup, so Setup can detect a running copy and offer to close it
; rather than failing halfway through with files in use.
AppMutex=WinNookSingleInstanceMutex
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Shortcuts:"; Flags: unchecked
Name: "startup";     Description: "Start {#AppName} when I sign in"; GroupDescription: "Startup:"

[Files]
; The whole publish folder. A self-contained build carries its own .NET runtime, so there is no
; prerequisite for the user to install.
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}";              Filename: "{app}\{#AppExe}"
Name: "{group}\Uninstall {#AppName}";    Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}";        Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Registry]
; The app manages this key itself from the Options tab. Setup only seeds it, and clears it on
; uninstall so a removed app cannot keep trying to launch at sign-in.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; \
    ValueName: "WinNook"; ValueData: """{app}\{#AppExe}"""; Flags: uninsdeletevalue; Tasks: startup
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: none; \
    ValueName: "WinNook"; Flags: deletevalue uninsdeletevalue; Tasks: not startup

[Run]
Filename: "{app}\{#AppExe}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent

[Code]
{ Settings, the shelf, and the widget databases live outside the install folder and survive an
  uninstall by default. Deleting someone's saved notes and clipboard history without asking would
  be the wrong default, so ask, and only on a real uninstall rather than an upgrade. }
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  RoamingData: String;
  LocalData: String;
  Gap: String;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    RoamingData := ExpandConstant('{userappdata}\WinNook');
    LocalData   := ExpandConstant('{localappdata}\WinNook');

    if DirExists(RoamingData) or DirExists(LocalData) then
    begin
      { Built with Chr(): Pascal Script has no sLineBreak, and a continuation line that starts
        with '#13' is swallowed by the preprocessor as a directive. }
      Gap := Chr(13) + Chr(10) + Chr(13) + Chr(10);

      if MsgBox('Remove your WinNook settings, shelf items, notes and clipboard history too?' +
                Gap + 'Choose No to keep them for a future reinstall.',
                mbConfirmation, MB_YESNO) = IDYES then
      begin
        DelTree(RoamingData, True, True, True);
        DelTree(LocalData, True, True, True);
      end;
    end;
  end;
end;
