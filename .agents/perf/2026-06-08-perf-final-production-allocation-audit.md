# Final Production Allocation Audit

Date: 2026-06-08
Version: v0.1.77

## Goal

Classify remaining production allocation and native-work search hits after the optimization suite.

## Search Scope

The audit scanned C# source for LINQ materialization, collection allocation, settings I/O, timers, dispatcher work, process launches, and asynchronous animation work.

## Findings

| Category | Current State | Outcome |
|----------|---------------|---------|
| Settings persistence | Load/save still uses JSON serialization and file I/O. Unchanged in-process saves now use a serialized payload and timestamp cache. | Necessary I/O; no further change. |
| Taskbar apply collections | Apply targets, alpha maps, dedupe sets, animation tracks, and live handle sets are pre-sized and serve real native apply work. | Expected materialization; no further change. |
| Animation loop | Frame-loop dictionary lookups were replaced with precomputed animation tracks. Remaining Task.Run and Task.Delay are the animation driver. | Necessary async loop; no further change. |
| Runtime sensor | Timer and native checks are gated by automation settings, hover priority, and reusable class-name buffer. | Necessary runtime sensing; no further change. |
| UI page row lists | Dashboard, diagnostics, and monitor pages allocate pre-sized view rows for XAML binding. Refreshes are coalesced. | UI binding work; no further change. |
| User actions | Process.Start and reset-settings writes happen from explicit commands. | Not a hot path. |
| Tests/helpers | Remaining OrderBy/ToArray and several collection allocations are test-only. Some helper methods are reached only through ForTest facades. | Not production work. |

## Conclusion

No additional production hot-path allocation or native-work issue was justified by the final scan. The remaining hits are necessary, already pre-sized, UI-binding related, explicit user actions, or test-only.

## Validation

- Regenerate and validate tracker workbook.
- Build Release x64.
- Run the test suite.
- Launch the desktop app and confirm the process is responsive.
