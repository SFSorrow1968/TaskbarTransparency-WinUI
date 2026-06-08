# Performance Report: Loaded Page Refresh Queueing
Date: 2026-06-08
Mode: optimize
Language: C# / WinUI / .NET 10

## Summary
Loaded pages previously queued a full refresh for every AppState.Changed notification. This pass added RefreshCoalescer so each page can have at most one pending dispatcher refresh, collapsing quick notification bursts into one UI update.

## Baseline Metrics
| Metric | Value |
|--------|-------|
| Dedicated benchmark harness | Not present |
| Affected page handlers | 6 |
| Duplicate refreshes possible while pending | Yes |
| Correctness baseline | Clean main at v0.1.52 |

## Hotspots
1. Pages/HomePage.xaml.cs: direct dispatcher refresh enqueue on each AppState.Changed.
2. Pages/PresetsPage.xaml.cs: same pattern.
3. Pages/MonitorsPage.xaml.cs: same pattern.
4. Pages/AutomationPage.xaml.cs: same pattern.
5. Pages/DiagnosticsPage.xaml.cs: same pattern.
6. Pages/SettingsPage.xaml.cs: same pattern.

## Findings
- What: State-listening pages queued duplicate refreshes during bursts.
- Why: Several settings and runtime operations can notify quickly; once a refresh is queued, additional pending refreshes are redundant for the current state snapshot.
- Fix: Add RefreshCoalescer and let pages queue only one pending dispatcher refresh at a time.

## Optimizations Applied
| Change | Before | After | Improvement |
|--------|--------|-------|-------------|
| Loaded page refresh queueing | One queued refresh per AppState.Changed event | One pending queued refresh per page | Reduces duplicate queued UI work during notification bursts |

## After Metrics
| Metric | Value |
|--------|-------|
| Dedicated benchmark harness | Not present |
| Affected page handlers | 6 |
| Duplicate refreshes possible while pending | No |
| Correctness validation | Release build, unit tests, workbook parser, live launcher |

## Recommendations
Continue with focused passes only where source inspection still finds repeated work. If future opportunities are not obvious, add a lightweight diagnostic counter or BenchmarkDotNet harness before further micro-optimizing.
