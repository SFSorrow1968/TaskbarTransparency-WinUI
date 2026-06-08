# Oxygen Taskbar

A from-scratch WinUI 3 rebuild of TaskbarTransparency. The app keeps the original idea, dynamic Windows taskbar material control, and reshapes it into a calmer desktop utility with a fluid dashboard, simple presets, automation policies, monitor awareness, diagnostics, and persistent settings.

## Current build

- WinUI 3 navigation shell with Mica backdrop
- Dashboard with live opacity, profile, and runtime controls
- Tuning screen for Clear, Acrylic glass, Mica, Solid, opacity, fade timing, hover proximity, startup, tray, and hotkey settings
- Taskbar composition interop for primary and secondary taskbars
- Automation policy model for desktop, hover, maximized, fullscreen, and visible-window states
- Settings persistence under `%LocalAppData%\OxygenTaskbar\settings.json`
- Monitor screen with detected display overview and per-display overrides
- Diagnostics screen for runtime timeline, recovery actions, launcher logs, and hotkey registration status
- Unit tests for settings persistence, taskbar appearance, runtime sensors, hotkeys, tray commands, policy, and snapshots
- Tracker workbook generated from `tracker/build_tracker.py`
- Generated mockups organized under `SCREEN IMAGES/`
- Self-contained unpackaged Windows App SDK output for local launcher runs

## Build

```powershell
dotnet build .\TaskbarTransparency.csproj -c Debug -p:Platform=x64
dotnet test .\tests\TaskbarTransparency.Tests\TaskbarTransparency.Tests.csproj -c Debug
```

## Run

```powershell
.\launch-taskbar-transparency.bat
```

Launcher logs are written to `launcher-logs\build-and-launch-latest.txt` and `launcher-logs\build-warnings-errors-latest.txt`.

## Release snapshot

Every completed implementation pass should publish a SemVer snapshot branch and tag:

```powershell
pwsh .\scripts\snapshot-release.ps1 -Version v0.1.0 -Push
```

The script creates branch `snapshot/vX.Y.Z`, commit message `snapshot: vX.Y.Z`, and annotated tag `vX.Y.Z`.
