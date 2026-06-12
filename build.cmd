@echo off
setlocal
echo ===== Building Sboard 추출기 (self-contained) =====
dotnet publish SboardExtractor.csproj -c Release -r win-x64 --self-contained true -o publish
if %errorlevel% neq 0 exit /b %errorlevel%

echo ===== Building Setup.exe (Inno Setup) =====
"%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe" installer.iss
if %errorlevel% neq 0 exit /b %errorlevel%

echo ===== Done =====
