1. Choose a SemVer version `vX.Y.Z`.
2. Always run `pwsh ./scripts/snapshot-release.ps1 -Version vX.Y.Z -Push` from the repository root after a completed implementation change set.
3. Publishing a snapshot branch and annotated tag is mandatory for completed implementation work.
4. The snapshot must publish branch `snapshot/vX.Y.Z` and annotated tag `vX.Y.Z` to `https://github.com/SFSorrow1968/TaskbarTransparency-WinUI`.
5. The snapshot commit message must be `snapshot: vX.Y.Z`.
6. The snapshot commit must include the full tracked build state, including `AGENTS.md`.
7. Never force-push a snapshot branch and never move an existing release tag; publish a new SemVer instead.
8. Launcher build logs are written to `launcher-logs\build-and-launch-latest.txt`.
9. Launcher build warning/error summaries are written to `launcher-logs\build-warnings-errors-latest.txt`.
10. Runtime settings are written to `%LocalAppData%\OxygenTaskbar\settings.json`.
11. Every run of `launch-taskbar-transparency.bat` must stop any existing `TaskbarTransparency.exe` process, delete previous launcher logs, and overwrite them with the new run output.
