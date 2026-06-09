# Startup Taskbar Catalog Reuse

Date: 2026-06-08
Version: v0.1.73

## Goal

Reduce duplicate native taskbar window discovery during app startup.

## Change

- Captured the startup taskbar window list once in AppState.Initialize.
- Let MonitorCatalog build profiles from a provided taskbar snapshot.
- Let TaskbarAppearanceService apply against a provided taskbar snapshot for the first launch apply.

## Expected Effect

- Avoids one redundant startup taskbar enumeration before the first window is shown.
- Preserves fresh live discovery for later applies, sensor ticks, and manual reapply actions.
- Keeps monitor profile and apply behavior aligned because both startup steps use the same taskbar snapshot.

## Validation

- Regenerate and validate tracker workbook.
- Build Release x64.
- Run the test suite, including provided taskbar snapshot coverage.
- Launch the desktop app and confirm the process is responsive.
