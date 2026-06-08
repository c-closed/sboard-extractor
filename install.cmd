@echo off
chcp 65001 >nul
title Sboard 추출기 설치

echo Sboard 추출기 설치를 시작합니다...
echo.

if not "%PROCESSOR_ARCHITECTURE%"=="AMD64" if not "%PROCESSOR_ARCHITEW6432%"=="AMD64" (
    echo 64비트 Windows가 필요합니다.
    pause
    exit /b 1
)

set "INSTALL_DIR=%ProgramFiles%\Sboard 추출기"

echo 설치 경로: %INSTALL_DIR%
echo.

if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"

echo 파일 복사중...
copy /y "%~dp0Sboard 추출기.exe" "%INSTALL_DIR%" > nul
copy /y "%~dp0Updater.exe" "%INSTALL_DIR%" > nul
copy /y "%~dp0_Sboard 추출기.cmd" "%INSTALL_DIR%" > nul

echo 바로가기 생성중...
if exist "%PUBLIC%\Desktop" (
    copy /y "%INSTALL_DIR%\_Sboard 추출기.cmd" "%PUBLIC%\Desktop\Sboard 추출기.lnk" > nul 2>&1
)
powershell -Command "$ws = New-Object -ComObject WScript.Shell; $s = $ws.CreateShortcut([Environment]::GetFolderPath('Desktop') + '\Sboard 추출기.lnk'); $s.TargetPath = '%INSTALL_DIR%\Sboard 추출기.exe'; $s.WorkingDirectory = '%INSTALL_DIR%'; $s.Save()" > nul 2>&1

echo.
echo 설치 완료! 바탕화면의 바로가기를 실행하세요.
pause
