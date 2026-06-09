# Monitor Refresh Merge Loop Optimization

Date: 2026-06-08
Version: v0.1.63

## Goal

Reduce small repeated allocation overhead during monitor refresh merging.

## Change

- Added `MonitorProfile.MergeDetectedList` for merging detected monitors with saved settings.
- Replaced `Select(...).ToList()` and `FirstOrDefault(...)` in `AppState.RefreshMonitors`.
- The merge now uses a pre-sized list and direct ordinal device-name lookup loops.

## Expected Effect

- Avoids LINQ iterator/delegate overhead in monitor refresh.
- Keeps saved monitor override preservation intact.
- Keeps stale saved monitors out of the live detected monitor list.

## Validation

- Regenerate and validate tracker workbook.
- Build Release x64.
- Run focused test suite with monitor merge coverage.
- Launch the desktop app and confirm the process is responsive.
