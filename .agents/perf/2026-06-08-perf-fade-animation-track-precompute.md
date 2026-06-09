# Fade Animation Track Precompute

Date: 2026-06-08
Version: v0.1.74

## Goal

Reduce per-frame lookup work in taskbar alpha fade animations.

## Change

- Replaced separate start-alpha and duration dictionaries in the animation loop with precomputed alpha animation tracks.
- Each track stores handle, start alpha, target alpha, and duration.
- Added focused coverage for duration precomputation.

## Expected Effect

- Avoids keyed dictionary lookups for every taskbar on every animation frame.
- Keeps the cancellation and frame delay behavior unchanged.
- Preserves direct alpha applies when no animated duration is needed.

## Validation

- Regenerate and validate tracker workbook.
- Build Release x64.
- Run the test suite, including precomputed animation duration coverage.
- Launch the desktop app and confirm the process is responsive.
