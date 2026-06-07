@echo off
setlocal
cd /d "%~dp0"

if not exist launcher-logs mkdir launcher-logs

taskkill /IM TaskbarTransparency.exe /F >nul 2>&1
del /Q launcher-logs\build-and-launch-latest.txt >nul 2>&1
del /Q launcher-logs\build-warnings-errors-latest.txt >nul 2>&1

echo Building Oxygen Taskbar... > launcher-logs\build-and-launch-latest.txt
dotnet build TaskbarTransparency.csproj -c Debug -p:Platform=x64 >> launcher-logs\build-and-launch-latest.txt 2>&1
if errorlevel 1 (
  findstr /I /C:"error" /C:"warning" launcher-logs\build-and-launch-latest.txt > launcher-logs\build-warnings-errors-latest.txt
  exit /b 1
)

findstr /I /C:"error" /C:"warning" launcher-logs\build-and-launch-latest.txt > launcher-logs\build-warnings-errors-latest.txt
echo Launching Oxygen Taskbar... >> launcher-logs\build-and-launch-latest.txt
start "" "%~dp0bin\x64\Debug\net10.0-windows10.0.26100.0\win-x64\TaskbarTransparency.exe"
endlocal
