# Monitor Page Scan Fast Paths

Date: 2026-06-08
Version: v0.1.66

## Goal

Reduce small delegate/enumerator overhead during Monitors page refresh.

## Change

- Added `MonitorProfile.CountSynced` with a list-backed direct loop.
- Added `MonitorProfile.SelectSecondaryOrPrimary` to preserve the page's secondary-first selection behavior.
- Replaced Monitors page `Count(predicate)` and two `FirstOrDefault(...)` scans with the shared helpers.

## Expected Effect

- Avoids delegate-based LINQ scans during repeated Monitors page refreshes.
- Preserves selection behavior for secondary displays and primary-only systems.
- Adds focused model-level test coverage for the helper behavior.

## Validation

- Regenerate and validate tracker workbook.
- Build Release x64.
- Run focused test suite with monitor helper coverage.
- Launch the desktop app and confirm the process is responsive.
