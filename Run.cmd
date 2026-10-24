@echo off
cd /d "%~dp0"
if exist "desktop\release\KnowYourDevice-2.0.0-Windows-x64.exe" (
    start "" "%~dp0desktop\release\KnowYourDevice-2.0.0-Windows-x64.exe"
    exit /b 0
)
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1"
if errorlevel 1 (pause & exit /b 1)
cd /d "%~dp0desktop"
call npm.cmd start
