@echo off
chcp 65001 >nul
title Sboard 추출기

reg query "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" /v Release 2>nul | findstr /C:"528040" >nul
if errorlevel 1 (
    echo .NET Framework 4.8이 설치되어 있지 않습니다. 설치를 진행합니다...
    echo 다운로드중...
    powershell -Command "& {[Net.ServicePointManager]::SecurityProtocol = 3072; Invoke-WebRequest -Uri 'https://go.microsoft.com/fwlink/?linkid=2088631' -OutFile '%TEMP%\ndp48-web.exe'}" >nul 2>&1
    if exist "%TEMP%\ndp48-web.exe" (
        echo 설치중...
        start /wait "%TEMP%\ndp48-web.exe" /q /norestart
        del "%TEMP%\ndp48-web.exe"
    ) else (
        echo 다운로드 실패. 브라우저를 엽니다.
        start https://go.microsoft.com/fwlink/?linkid=2088631
        pause
        exit /b
    )
)
start "" "%~dp0SboardExtractor.exe"
