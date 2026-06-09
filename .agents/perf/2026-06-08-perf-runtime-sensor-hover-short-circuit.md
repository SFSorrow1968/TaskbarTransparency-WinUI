# Runtime Sensor Hover Priority Short-Circuit

Date: 2026-06-08
Version: v0.1.70

## Goal

Reduce unnecessary native foreground/window checks during hover-active sensor ticks.

## Change

- Moved hover proximity detection ahead of foreground/maximized/fullscreen state detection.
- Return `AutomationTrigger.Hover` immediately when hover reveal is enabled and the pointer is near a taskbar.
- Kept existing automation disabled and hover disabled gates.

## Expected Effect

- Avoids foreground, class-name, maximized, and fullscreen native checks when hover already determines the trigger.
- Preserves existing trigger priority.
- Keeps the once-per-second runtime sensor path simpler in hover-active states.

## Validation

- Regenerate and validate tracker workbook.
- Build Release x64.
- Run focused test suite, including existing hover-priority trigger tests.
- Launch the desktop app and confirm the process is responsive.
