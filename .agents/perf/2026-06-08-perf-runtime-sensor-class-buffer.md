# Runtime Sensor Shell Class Buffer Reuse

Date: 2026-06-08
Version: v0.1.71

## Goal

Reduce avoidable allocation during foreground shell-window checks in the once-per-second runtime sensor tick.

## Change

- Added a reusable class-name StringBuilder on RuntimeStateSensorService.
- Reused the buffer before each GetClassName call.
- Extracted shell class matching into a testable helper.

## Expected Effect

- Avoids allocating a new StringBuilder and backing buffer on every shell-window class lookup.
- Preserves exact shell class filtering behavior.
- Keeps the interop signature stable.

## Validation

- Regenerate and validate tracker workbook.
- Build Release x64.
- Run focused test suite with shell class-name coverage.
- Launch the desktop app and confirm the process is responsive.
