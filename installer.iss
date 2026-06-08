[Setup]
AppName=Sboard 추출기
AppVersion=1.0.0.0
DefaultDirName={pf}\Sboard 추출기
DefaultGroupName=Sboard 추출기
UninstallDisplayIcon={app}\Sboard 추출기.exe
Compression=lzma2
SolidCompression=yes
OutputDir=.
OutputBaseFilename=Sboard 추출기_Setup

[Files]
Source: "Sboard 추출기.exe"; DestDir: "{app}"
Source: "Updater.exe"; DestDir: "{app}"
Source: "_Sboard 추출기.cmd"; DestDir: "{app}"

[Icons]
Name: "{commondesktop}\Sboard 추출기"; Filename: "{app}\Sboard 추출기.exe"
Name: "{group}\Sboard 추출기"; Filename: "{app}\Sboard 추출기.exe"
Name: "{group}\제거"; Filename: "{uninstallexe}"
