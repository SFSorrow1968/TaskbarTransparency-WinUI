# Runtime Sensor Native Check Gating

Date: 2026-06-08
Version: v0.1.69

## Goal

Reduce unnecessary native window/taskbar polling during the once-per-second runtime sensor tick.

## Change

- Read settings before native foreground/taskbar checks.
- Return `Desktop` immediately when automation is disabled.
- Run fullscreen detection only when fullscreen overlap is enabled.
- Run taskbar proximity detection only when hover reveal is enabled.

## Expected Effect

- Avoids native work when automation is paused.
- Avoids taskbar enumeration/cursor proximity checks when hover reveal is off.
- Avoids fullscreen monitor/window-rect checks when fullscreen overlap is off.
- Preserves existing `ResolveTrigger` priority and behavior.

## Validation

- Regenerate and validate tracker workbook.
- Build Release x64.
- Run focused test suite, including existing runtime trigger priority tests.
- Launch the desktop app and confirm the process is responsive.
