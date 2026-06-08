# Performance Report: Runtime Refresh History Lists
Date: 2026-06-08
Mode: optimize
Language: C# / WinUI / .NET 10

## Summary
Dashboard, Monitors, and Diagnostics were doing repeated string-signature construction across recent runtime events during UI refresh. This pass replaced those signatures with a RuntimeSnapshot event-version counter so unchanged history lists are checked with one integer comparison.

## Baseline Metrics
| Metric | Value |
|--------|-------|
| Dedicated benchmark harness | Not present |
| Affected refresh paths | 3 pages |
| Max events scanned per refresh before | 8 |
| Signature allocation before | string.Join plus interpolated fragments per refresh |
| Correctness baseline | Clean main at v0.1.50 |

## Hotspots
1. Pages/HomePage.xaml.cs: recent runtime list refresh created a joined event signature string.
2. Pages/MonitorsPage.xaml.cs: recent monitor actions did the same.
3. Pages/DiagnosticsPage.xaml.cs: sensor timeline did the same.

## Findings
- What: Runtime history list freshness checks rebuilt string signatures on every state refresh.
- Why: Frequent runtime sensor updates and app-state notifications can refresh loaded pages often; the list only changes when RuntimeSnapshot.RecordEvent adds an event.
- Fix: Add RuntimeSnapshot.RecentEventsVersion and increment it in RecordEvent, then let pages compare integers before rebuilding ItemsSource.

## Optimizations Applied
| Change | Before | After | Improvement |
|--------|--------|-------|-------------|
| Runtime history freshness checks | Rebuild joined signature strings from recent events per refresh | Compare RecentEventsVersion integer | Removes repeated signature-string allocation and event scan when history is unchanged |

## After Metrics
| Metric | Value |
|--------|-------|
| Dedicated benchmark harness | Not present |
| Affected refresh paths | 3 pages |
| Max events scanned per unchanged refresh after | 0 |
| Signature allocation after | None for unchanged refresh |
| Correctness validation | Release build, unit tests, workbook parser, live launcher |

## Recommendations
Continue with measured passes over other refresh paths that still compute string signatures or recreate UI rows. Add a small BenchmarkDotNet or focused allocation harness later if optimization work moves from obvious repeated allocations into less visible runtime tradeoffs.
