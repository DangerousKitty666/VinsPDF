#define AppVersion "0.30"
[Setup]
AppId={{938C0682-DAEF-4CC9-BCDB-CCB54B76FF2D}
AppName=VinsPDF
AppVersion={#AppVersion}
AppPublisher=DangerousKitty666
AppPublisherURL=https://github.com/DangerousKitty666/VinsPDF
AppUpdatesURL=https://github.com/DangerousKitty666/VinsPDF/releases
DefaultDirName={code:DefaultFolder}
DefaultGroupName=VinsPDF
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\..
OutputBaseFilename=VinsPDF-Setup-{#AppVersion}
SetupIconFile=..\source\VinsPDF.ico
UninstallDisplayIcon={app}\VinsPDF.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern dark
DisableProgramGroupPage=yes
CloseApplications=no
RestartApplications=no
ChangesAssociations=yes
Uninstallable=not TestInstall
CreateUninstallRegKey=not TestInstall
[Files]
Source: "..\VinsPDF.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\VinsPDF.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\qpdf\*"; DestDir: "{app}\qpdf"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\pdfium.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\VinsPDF-document.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\README.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\licenses\*"; DestDir: "{app}\licenses"; Flags: ignoreversion recursesubdirs createallsubdirs
[Icons]
Name: "{userprograms}\VinsPDF"; Filename: "{app}\VinsPDF.exe"; Check: not TestInstall
Name: "{userdesktop}\VinsPDF"; Filename: "{app}\VinsPDF.exe"; Check: not TestInstall
[Registry]
Root: HKCU; Subkey: "Software\Classes\VinsPDF.Document"; ValueType: string; ValueName: ""; ValueData: "VinsPDF PDF document"; Flags: uninsdeletekey; Check: not TestInstall
Root: HKCU; Subkey: "Software\Classes\VinsPDF.Document\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: """{app}\VinsPDF-document.ico"",0"; Check: not TestInstall
Root: HKCU; Subkey: "Software\Classes\VinsPDF.Document\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\VinsPDF.exe"" ""%1"""; Check: not TestInstall
Root: HKCU; Subkey: "Software\Classes\.pdf\OpenWithProgids"; ValueType: string; ValueName: "VinsPDF.Document"; ValueData: ""; Flags: uninsdeletevalue; Check: not TestInstall
Root: HKCU; Subkey: "Software\Classes\Applications\VinsPDF.exe"; ValueType: string; ValueName: "FriendlyAppName"; ValueData: "VinsPDF"; Flags: uninsdeletekey; Check: not TestInstall
Root: HKCU; Subkey: "Software\Classes\Applications\VinsPDF.exe\SupportedTypes"; ValueType: string; ValueName: ".pdf"; ValueData: ""; Check: not TestInstall
Root: HKCU; Subkey: "Software\Classes\Applications\VinsPDF.exe\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\VinsPDF.exe"" ""%1"""; Check: not TestInstall
Root: HKCU; Subkey: "Software\Classes\Applications\VinsPDF.exe\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: """{app}\VinsPDF.exe"",0"; Check: not TestInstall
Root: HKCU; Subkey: "Software\VinsPDF\Capabilities"; ValueType: string; ValueName: "ApplicationName"; ValueData: "VinsPDF"; Flags: uninsdeletekey; Check: not TestInstall
Root: HKCU; Subkey: "Software\VinsPDF\Capabilities"; ValueType: string; ValueName: "ApplicationDescription"; ValueData: "Lightweight PDF viewer and editor"; Check: not TestInstall
Root: HKCU; Subkey: "Software\VinsPDF\Capabilities"; ValueType: string; ValueName: "ApplicationIcon"; ValueData: """{app}\VinsPDF.exe"",0"; Check: not TestInstall
Root: HKCU; Subkey: "Software\VinsPDF\Capabilities\FileAssociations"; ValueType: string; ValueName: ".pdf"; ValueData: "VinsPDF.Document"; Check: not TestInstall
Root: HKCU; Subkey: "Software\RegisteredApplications"; ValueType: string; ValueName: "VinsPDF"; ValueData: "Software\VinsPDF\Capabilities"; Flags: uninsdeletevalue; Check: not TestInstall
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\App Paths\VinsPDF.exe"; ValueType: string; ValueName: ""; ValueData: "{app}\VinsPDF.exe"; Flags: uninsdeletekey; Check: not TestInstall
[Run]
Filename: "{app}\VinsPDF.exe"; Description: "Open VinsPDF"; Flags: nowait postinstall; Check: ShouldLaunch
[Code]
function OpenProcess(Access: LongWord; Inherit: Boolean; PID: LongWord): THandle; external 'OpenProcess@kernel32.dll stdcall';
function WaitForSingleObject(Handle: THandle; Milliseconds: LongWord): LongWord; external 'WaitForSingleObject@kernel32.dll stdcall';
function CloseHandle(Handle: THandle): Boolean; external 'CloseHandle@kernel32.dll stdcall';
function TestInstall: Boolean;
begin Result := ExpandConstant('{param:TESTINSTALL|0}') = '1'; end;
function ShouldLaunch: Boolean;
begin Result := (not TestInstall) and (ExpandConstant('{param:NORUN|0}') <> '1'); end;
function DefaultFolder(Param: String): String;
begin
 if TestInstall then Result := ExpandConstant('{param:DIR}') else Result := ExpandConstant('{localappdata}\Programs\VinsPDF');
end;
function InitializeSetup: Boolean;
var PID: Integer; Handle: THandle;
begin
  Result := True;
  PID := StrToIntDef(ExpandConstant('{param:WAITPID|0}'), 0);
  if PID > 0 then begin
    Handle := OpenProcess($00100000, False, PID);
    if Handle <> 0 then begin
      Result := WaitForSingleObject(Handle, 60000) = 0;
      CloseHandle(Handle);
      if not Result then MsgBox('VinsPDF is still open. Save your documents and close it, then run the update again.', mbInformation, MB_OK);
    end;
  end;
end;

