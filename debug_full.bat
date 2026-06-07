@echo off
chcp 65001 >nul
echo === Full flow debug ===
"%~dp0SboardDebug.exe" --console
pause
