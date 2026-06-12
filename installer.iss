[Setup]
AppName=Sboard 추출기
AppVersion=1.2.0.0
DefaultDirName={autopf}\Sboard 추출기
DefaultGroupName=Sboard 추출기
UninstallDisplayIcon={app}\Sboard 추출기.exe
Compression=lzma2
SolidCompression=yes
OutputDir=.
OutputBaseFilename=Sboard 추출기_Setup
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible
DisableProgramGroupPage=yes

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autodesktop}\Sboard 추출기"; Filename: "{app}\Sboard 추출기.exe"
Name: "{autoprograms}\Sboard 추출기"; Filename: "{app}\Sboard 추출기.exe"
Name: "{autoprograms}\Sboard 추출기 제거"; Filename: "{uninstallexe}"
