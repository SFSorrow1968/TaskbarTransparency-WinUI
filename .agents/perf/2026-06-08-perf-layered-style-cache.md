# Layered Style Check Cache

Date: 2026-06-08
Version: v0.1.75

## Goal

Reduce repeated native style reads during layered alpha applies.

## Change

- Added a per-handle layered-style cache in TaskbarAppearanceService.
- Marked handles as known layered after style verification or setup.
- Pruned the layered-style cache with stale taskbar handles.
- Added focused helper coverage for when layered-style reads are needed.

## Expected Effect

- Avoids repeated GetWindowLongPtr calls for taskbar handles already known to be layered.
- Preserves first-use style setup for new taskbar handles.
- Keeps stale handle cleanup aligned with existing alpha and appearance caches.

## Validation

- Regenerate and validate tracker workbook.
- Build Release x64.
- Run the test suite, including layered-style read decision coverage.
- Launch the desktop app and confirm the process is responsive.
