#define MyAppName "Quark"
#define MyAppVersion "0.1.0"
#define MyAppPublisher "Quark contributors"
#define MyAppExeName "Quark.exe"
#define MySourceDir "..\src\Quark.App\bin\Release\net8.0-windows\win-x64\publish"

[Setup]
AppId={{26EC5582-EC57-4D67-BCBE-73F494453F4C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=..\LICENSE
OutputDir=..\dist
OutputBaseFilename=Quark-Setup-{#MyAppVersion}-win-x64
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
SetupArchitecture=x64
SetupIconFile=..\assets\quark.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
CloseApplications=yes
CloseApplicationsFilter={#MyAppExeName}
PrivilegesRequired=lowest

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked
Name: "launch"; Description: "Launch Quark after installation"; GroupDescription: "After installation:"; Flags: checkedonce

[Files]
Source: "{#MySourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Quark"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\Quark"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch Quark"; Flags: nowait postinstall skipifsilent; Tasks: launch
