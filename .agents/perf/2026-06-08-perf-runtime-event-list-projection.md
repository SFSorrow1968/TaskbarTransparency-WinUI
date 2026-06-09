# Runtime Event List Projection Allocation Pass

Date: 2026-06-08
Version: v0.1.67

## Goal

Reduce small allocation overhead when runtime event lists are rebuilt on UI pages.

## Change

- Replaced `Select(...).ToList()` projections in Dashboard recent events.
- Replaced `Select(...).ToList()` projections in Diagnostics sensor timeline.
- Replaced monitor overview and recent monitor action list projections with pre-sized loops.

## Expected Effect

- Avoids LINQ iterator/delegate allocation during version-gated list rebuilds.
- Preserves existing row text and binding behavior.
- Keeps existing event-version guards, so unchanged lists still skip work.

## Validation

- Regenerate and validate tracker workbook.
- Build Release x64.
- Run the focused test suite.
- Launch the desktop app and confirm the process is responsive.
