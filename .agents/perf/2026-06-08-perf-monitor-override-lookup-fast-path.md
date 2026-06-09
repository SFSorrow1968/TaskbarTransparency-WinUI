# Monitor Override Lookup Fast Path

Date: 2026-06-08
Version: v0.1.65

## Goal

Reduce small allocation and delegate overhead when applying per-monitor overrides.

## Change

- Added a shared `MonitorProfile.FindByDeviceName` helper.
- The helper uses direct index lookup for list-backed collections and preserves enumerable fallback behavior.
- Replaced the two `FirstOrDefault(...)` monitor searches in `AppState.SetMonitorOverride`.

## Expected Effect

- Avoids delegate-based LINQ searches in the monitor override apply path.
- Preserves case-insensitive UI override matching.
- Keeps exact ordinal matching for detected/saved monitor merges.

## Validation

- Regenerate and validate tracker workbook.
- Build Release x64.
- Run focused test suite with monitor lookup comparison coverage.
- Launch the desktop app and confirm the process is responsive.
