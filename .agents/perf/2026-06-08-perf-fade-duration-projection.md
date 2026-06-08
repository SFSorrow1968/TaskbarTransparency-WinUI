# Fade Duration Projection Allocation Pass

Date: 2026-06-08
Version: v0.1.61

## Goal

Reduce managed allocation overhead while preparing taskbar fade animations.

## Change

- Replaced `targetAlphas.ToDictionary(...)` with a pre-sized loop for animation start alphas.
- Replaced duration `ToDictionary(...)` with a pre-sized loop in `SelectFadeDurations`.
- Replaced `fadeDurations.Values.All(...)` with a direct scan that exits as soon as an animated duration is found.

## Expected Effect

- Avoids LINQ iterator/delegate overhead during fade setup.
- Preserves the same start-alpha fallback and fade-in/fade-out duration selection.
- Keeps the instant-apply fast path for unchanged or zero-duration fades.

## Validation

- Regenerate and validate tracker workbook.
- Build Release x64.
- Run focused test suite, including existing fade-duration tests.
- Launch the desktop app and confirm the process is responsive.
