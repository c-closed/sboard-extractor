@echo off
chcp 65001 >nul
set REFDIR=C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8
set CSC=C:\Program Files\dotnet\sdk\10.0.300\Roslyn\bincore\csc.exe
set SRC=%~dp0SboardExtractor.cs
set OUT=%~dp0SboardExtractor_new.exe

"%CSC%" /target:winexe /nologo /lib:"%REFDIR%" ^
  /reference:mscorlib.dll ^
  /reference:System.dll ^
  /reference:System.Core.dll ^
  /reference:System.Windows.Forms.dll ^
  /reference:System.Drawing.dll ^
  /reference:System.Xml.dll ^
  /reference:System.Data.dll ^
  /reference:Microsoft.CSharp.dll ^
  /reference:System.IO.Compression.dll ^
  /reference:System.IO.Compression.FileSystem.dll ^
  "%SRC%" /out:"%OUT%"

if %errorlevel% equ 0 (
  echo Build successful: %OUT%
) else (
  echo Build failed (exit code %errorlevel%)
  pause
)
