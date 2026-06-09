# Settings Save Cache

Date: 2026-06-08
Version: v0.1.72

## Goal

Avoid repeated settings file reads during ordinary unchanged in-process saves while preserving external file change detection.

## Change

- Added cached serialized settings and last-write timestamp tracking in SettingsStore.
- Let Save return before reading settings.json when the serialized payload and file timestamp still match.
- Updated the cache after successful loads, existing-file matches, and atomic writes.

## Expected Effect

- Reduces disk reads during redundant or settled settings saves.
- Keeps external settings edits observable because timestamp changes fall back to the existing file-content comparison.
- Preserves temp-file atomic write behavior for changed settings.

## Validation

- Regenerate and validate tracker workbook.
- Build Release x64.
- Run the test suite, including external settings file change coverage.
- Launch the desktop app and confirm the process is responsive.
