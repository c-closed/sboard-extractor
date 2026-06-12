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

echo ===== Building Setup.exe (Inno Setup) =====
"%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe" installer.iss
if %errorlevel% neq 0 exit /b %errorlevel%

echo ===== Done =====
