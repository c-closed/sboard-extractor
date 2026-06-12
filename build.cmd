@echo off
setlocal
echo ===== Building Sboard 추출기 (self-contained) =====
dotnet publish SboardExtractor.csproj -c Release -r win-x64 --self-contained true -o publish
if %errorlevel% neq 0 exit /b %errorlevel%

echo ===== Building Updater =====
dotnet publish Updater.csproj -c Release -r win-x64 --self-contained true -o publish_tmp
if %errorlevel% neq 0 exit /b %errorlevel%

echo ===== Merging Updater into publish =====
copy /y "publish_tmp\Updater.exe" "publish\Updater.exe" >nul
copy /y "publish_tmp\Updater.dll" "publish\Updater.dll" >nul
copy /y "publish_tmp\Updater.pdb" "publish\Updater.pdb" >nul
copy /y "publish_tmp\Updater.deps.json" "publish\Updater.deps.json" >nul
copy /y "publish_tmp\Updater.runtimeconfig.json" "publish\Updater.runtimeconfig.json" >nul
if %errorlevel% neq 0 exit /b %errorlevel%
rmdir /s /q publish_tmp >nul

echo ===== Generating Files.wxs =====
powershell -NoProfile -Command ^
  "$p = (Resolve-Path 'publish').Path;" ^
  "$sb = New-Object System.Text.StringBuilder;" ^
  "$sb.AppendLine('<?xml version=\"1.0\" encoding=\"utf-8\"?>') | Out-Null;" ^
  "$sb.AppendLine('<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">') | Out-Null;" ^
  "$sb.AppendLine('  <Fragment>') | Out-Null;" ^
  "$sb.AppendLine('    <ComponentGroup Id=\"PublishFiles\">') | Out-Null;" ^
  "Get-ChildItem -LiteralPath $p -File | ForEach-Object {" ^
    "$fid = 'f_' + [System.Guid]::NewGuid().ToString('N');" ^
    "$cid = 'c_' + [System.Guid]::NewGuid().ToString('N');" ^
    "$sb.AppendLine('      <Component Id=\"' + $cid + '\" Directory=\"INSTALLDIR\" Guid=\"*\">') | Out-Null;" ^
    "$sb.AppendLine('        <File Id=\"' + $fid + '\" Source=\"' + $_.FullName + '\" KeyPath=\"yes\" />') | Out-Null;" ^
    "$sb.AppendLine('      </Component>') | Out-Null;" ^
  "};" ^
  "$sb.AppendLine('    </ComponentGroup>') | Out-Null;" ^
  "$sb.AppendLine('  </Fragment>') | Out-Null;" ^
  "$sb.AppendLine('</Wix>') | Out-Null;" ^
  "Set-Content -Path 'Files.wxs' -Value $sb.ToString() -Encoding utf8"
if %errorlevel% neq 0 exit /b %errorlevel%

echo ===== Building MSI =====
wix build Product.wxs Files.wxs -o "Sboard 추출기.msi" -pdbtype none --acceptEula yes
if %errorlevel% neq 0 exit /b %errorlevel%

echo ===== Done =====
