# Concurrent Cache Prune Enumeration

Date: 2026-06-08
Version: v0.1.76

## Goal

Reduce avoidable enumeration overhead while pruning stale taskbar apply caches.

## Change

- Changed alpha, appearance, and layered-style cache pruning to enumerate ConcurrentDictionary entries directly.
- Continued to use the existing live handle set and TryRemove cleanup.
- Kept stale cache behavior unchanged across all three apply caches.

## Expected Effect

- Avoids the separate Keys collection path for each cache during every apply cleanup.
- Keeps stale handle cleanup aligned across alpha, native appearance, and layered-style caches.
- Preserves concurrent-safe removal semantics.

## Validation

- Regenerate and validate tracker workbook.
- Build Release x64.
- Run the test suite.
- Launch the desktop app and confirm the process is responsive.
