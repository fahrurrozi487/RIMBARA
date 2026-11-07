@echo off
if not exist "%~dp0Builds\Windows\RIMBARA.exe" (
  echo Build RIMBARA with Unity's RIMBARA ^> Build Windows x64 menu first.
  pause
  exit /b 1
)
start "RIMBARA" "%~dp0Builds\Windows\RIMBARA.exe" %*
