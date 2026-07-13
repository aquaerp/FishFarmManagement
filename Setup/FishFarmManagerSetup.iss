; Script generated for FishFarmManager Installer
[Setup]
AppName=AquaFarm Pro
AppVersion=1.0.0
AppPublisher=AquaFarm Pro
DefaultDirName={autopf}\AquaFarm Pro
DefaultGroupName=AquaFarm Pro
OutputDir=..\artifacts\installer
OutputBaseFilename=AquaFarmPro-1.0.0-win-x64-setup
Compression=lzma
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\Logs\LICENSE.txt"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "..\Docs\Guides\UserGuide.md"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist

[Icons]
Name: "{group}\AquaFarm Pro"; Filename: "{app}\FishFarmManager.exe"
Name: "{group}\{cm:UninstallProgram,AquaFarm Pro}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\AquaFarm Pro"; Filename: "{app}\FishFarmManager.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\FishFarmManager.exe"; Description: "{cm:LaunchProgram,AquaFarm Pro}"; Flags: nowait postinstall skipifsilent
