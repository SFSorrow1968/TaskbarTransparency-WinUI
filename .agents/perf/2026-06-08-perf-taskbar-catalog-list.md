# Taskbar Catalog List Allocation Pass

Date: 2026-06-08
Version: v0.1.60

## Goal

Reduce managed allocation overhead in the taskbar window discovery path without changing taskbar enumeration behavior.

## Change

- Replaced the `yield return` iterator and `ToArray()` materialization in `TaskbarWindowCatalog.GetCurrent()`.
- The method now builds and returns a small pre-sized `List<TaskbarWindowInfo>` directly.
- Kept primary taskbar discovery first, followed by secondary taskbar enumeration through `FindWindowEx`.

## Expected Effect

- Avoids iterator state-machine allocation during catalog refresh.
- Avoids array materialization after enumeration.
- Keeps the existing read-only public contract and downstream behavior intact.

## Validation

- Regenerate and validate tracker workbook.
- Build Release x64.
- Run focused test suite.
- Launch the desktop app and confirm the process is responsive.
