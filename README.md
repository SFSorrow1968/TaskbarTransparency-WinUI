# Oxygen Taskbar

A WinUI 3 utility that controls Windows taskbar transparency with one simple model: a **base opacity**, plus optional **rules** that set an exact opacity for specific states (hover, fullscreen, maximized window, open window). What you set is what you see — there are no hidden adjustments.

## How it works

- **Base opacity** applies on the desktop and whenever no rule matches.
- **Rules** are absolute percentages evaluated top-down: Hover → Fullscreen → Maximized → Open window. The first enabled match wins; disabled rules fall through.
- **Automation** can be toggled off entirely, locking the taskbar to the base opacity.
- **Pause transparency** (tray, hotkey, or dashboard) temporarily restores the normal taskbar look without touching your settings.

## Screens

- **Dashboard** — applied opacity right now, which rule produced it, base opacity slider, automation and pause controls.
- **Rules** — edit each rule's toggle and opacity, plus hover distance.
- **Monitors** — every detected display; each can follow the base opacity or use its own value.
- **Settings** — fade duration, tray icon, start with Windows, keyboard shortcuts.
- **Diagnostics** — detection status, recent activity, shortcut registration, logs.
- **About** — version, repository, settings location.

Settings persist at `%LocalAppData%\OxygenTaskbar\settings.json`.

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
pwsh .\scripts\snapshot-release.ps1 -Version v0.2.0 -Push
```

The script creates branch `snapshot/vX.Y.Z`, commit message `snapshot: vX.Y.Z`, and annotated tag `vX.Y.Z`.
