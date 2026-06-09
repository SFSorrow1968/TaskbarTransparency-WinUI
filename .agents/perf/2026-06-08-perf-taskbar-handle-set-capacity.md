# Taskbar Handle Set Capacity Pass

Date: 2026-06-08
Version: v0.1.68

## Goal

Reduce small repeated allocation/growth overhead in the taskbar apply path.

## Change

- Pre-sized the dedupe `HashSet<IntPtr>` in `DistinctByHandle`.
- Pre-sized the live-handle `HashSet<IntPtr>` used during stale cache pruning.

## Expected Effect

- Avoids avoidable HashSet bucket growth when taskbar target counts are already known.
- Preserves dedupe order, primary taskbar preference, and stale cache pruning behavior.
- Keeps the change tightly scoped to repeated taskbar apply work.

## Validation

- Regenerate and validate tracker workbook.
- Build Release x64.
- Run focused test suite, including existing handle dedupe coverage.
- Launch the desktop app and confirm the process is responsive.
