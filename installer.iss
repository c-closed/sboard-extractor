[Setup]
AppId={{B4F5C6D7-E8F9-4A3B-8C2D-1E5F6A7B8C9D}
AppName=Sboard 추출기
AppVersion=1.4.2.0
DefaultDirName={autopf}\Sboard 추출기
DefaultGroupName=Sboard 추출기
UninstallDisplayIcon={app}\Sboard 추출기.exe
Compression=lzma2
SolidCompression=yes
OutputDir=.
OutputBaseFilename=Sboard 추출기_Setup
SetupIconFile=icon.ico
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible
DisableProgramGroupPage=yes
LanguageDetectionMethod=locale
ShowLanguageDialog=no

[Languages]
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autodesktop}\Sboard 추출기"; Filename: "{app}\Sboard 추출기.exe"
Name: "{autoprograms}\Sboard 추출기"; Filename: "{app}\Sboard 추출기.exe"
Name: "{autoprograms}\Sboard 추출기 제거"; Filename: "{uninstallexe}"

[Code]
var
  OriginalNewerVersion: string;

function GetUninstallKeyName: string;
begin
  Result := '{#SetupSetting("AppId")}_is1';
end;

function GetInstalledVersion: string;
begin
  Result := '';
  if RegQueryStringValue(HKLM64, 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\' + GetUninstallKeyName, 'DisplayVersion', Result) then
    Exit;
  RegQueryStringValue(HKLM32, 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\' + GetUninstallKeyName, 'DisplayVersion', Result);
end;

function ParseVersion(VersionStr: string; var Major, Minor, Build, Revision: Integer): Boolean;
var
  P: Integer;
begin
  Result := False;
  Major := 0; Minor := 0; Build := 0; Revision := 0;
  if VersionStr = '' then Exit;
  
  P := Pos('.', VersionStr);
  if P = 0 then begin Major := StrToIntDef(VersionStr, 0); Result := True; Exit; end;
  Major := StrToIntDef(Copy(VersionStr, 1, P - 1), 0);
  VersionStr := Copy(VersionStr, P + 1, Length(VersionStr) - P);
  
  P := Pos('.', VersionStr);
  if P = 0 then begin Minor := StrToIntDef(VersionStr, 0); Result := True; Exit; end;
  Minor := StrToIntDef(Copy(VersionStr, 1, P - 1), 0);
  VersionStr := Copy(VersionStr, P + 1, Length(VersionStr) - P);
  
  P := Pos('.', VersionStr);
  if P = 0 then begin Build := StrToIntDef(VersionStr, 0); Result := True; Exit; end;
  Build := StrToIntDef(Copy(VersionStr, 1, P - 1), 0);
  VersionStr := Copy(VersionStr, P + 1, Length(VersionStr) - P);
  
  Revision := StrToIntDef(VersionStr, 0);
  Result := True;
end;

function VersionCmp(Ver1, Ver2: string): Integer;
var
  Maj1, Min1, Bld1, Rev1: Integer;
  Maj2, Min2, Bld2, Rev2: Integer;
begin
  Result := 0;
  if not ParseVersion(Ver1, Maj1, Min1, Bld1, Rev1) then Exit;
  if not ParseVersion(Ver2, Maj2, Min2, Bld2, Rev2) then Exit;
  if Maj1 > Maj2 then Result := 1
  else if Maj1 < Maj2 then Result := -1
  else if Min1 > Min2 then Result := 1
  else if Min1 < Min2 then Result := -1
  else if Bld1 > Bld2 then Result := 1
  else if Bld1 < Bld2 then Result := -1
  else if Rev1 > Rev2 then Result := 1
  else if Rev1 < Rev2 then Result := -1;
end;

function InitializeSetup: Boolean;
var
  InstalledVersion: string;
  UninstallKey: string;
begin
  Result := True;
  OriginalNewerVersion := '';

  InstalledVersion := GetInstalledVersion;
  if InstalledVersion = '' then
    Exit;

  if VersionCmp(InstalledVersion, '{#SetupSetting("AppVersion")}') > 0 then
  begin
    if MsgBox('더 최신 버전의 Sboard 추출기가 이미 설치되어 있습니다.' #13#10
              '계속하면 복구/재설치합니다.' #13#10 #13#10
              '계속하시겠습니까?', mbConfirmation, MB_YESNO) = IDNO then
    begin
      Result := False;
    end
    else
    begin
      OriginalNewerVersion := InstalledVersion;
      UninstallKey := 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\' + GetUninstallKeyName;
      RegWriteStringValue(HKLM64, UninstallKey, 'DisplayVersion', '{#SetupSetting("AppVersion")}');
      RegWriteStringValue(HKLM32, UninstallKey, 'DisplayVersion', '{#SetupSetting("AppVersion")}');
    end;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  UninstallKey: string;
begin
  if (CurStep = ssPostInstall) and (OriginalNewerVersion <> '') then
  begin
    UninstallKey := 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\' + GetUninstallKeyName;
    RegWriteStringValue(HKLM64, UninstallKey, 'DisplayVersion', OriginalNewerVersion);
    RegWriteStringValue(HKLM32, UninstallKey, 'DisplayVersion', OriginalNewerVersion);
  end;
end;