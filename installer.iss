[Setup]
AppName=Taskbar Timer
AppVersion=1.0.0
AppPublisher=Taskbar Timer
DefaultDirName={autopf}\Taskbar Timer
DefaultGroupName=Taskbar Timer
PrivilegesRequired=lowest
OutputDir=installer
OutputBaseFilename=TaskbarTimerSetup
SetupIconFile=icon.ico
UninstallDisplayIcon={app}\TaskbarTimer.exe
CloseApplications=yes
ArchitecturesInstallIn64BitMode=x64compatible
Compression=lzma2
SolidCompression=yes

[Files]
Source: "publish\TaskbarTimer.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Taskbar Timer"; Filename: "{app}\TaskbarTimer.exe"

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueName: "TaskbarTimer"; ValueType: none; Flags: dontcreatekey uninsdeletevalue

[Run]
Filename: "{app}\TaskbarTimer.exe"; Description: "Launch Taskbar Timer after installation"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{userappdata}\TaskbarTimer"
