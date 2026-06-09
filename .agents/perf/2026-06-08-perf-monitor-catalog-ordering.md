# Monitor Catalog Ordering Allocation Pass

Date: 2026-06-08
Version: v0.1.64

## Goal

Reduce allocation overhead while building detected monitor profiles.

## Change

- Replaced the LINQ `OrderByDescending(...).ThenBy(...).ToArray()` pipeline in `MonitorCatalog`.
- Added a pre-sized direct profile builder.
- Primary profiles are placed first during insertion while discovery-based friendly names are preserved.

## Expected Effect

- Avoids ordering iterator/delegate overhead and array materialization.
- Preserves profile defaults for primary and secondary taskbars.
- Keeps monitor catalog behavior testable without requiring live Win32 taskbar discovery.

## Validation

- Regenerate and validate tracker workbook.
- Build Release x64.
- Run focused test suite with monitor catalog ordering coverage.
- Launch the desktop app and confirm the process is responsive.
