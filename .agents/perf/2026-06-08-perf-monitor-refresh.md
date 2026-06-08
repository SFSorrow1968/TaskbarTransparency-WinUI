# Performance Report: Monitor Overview Refresh
Date: 2026-06-08
Mode: optimize
Language: C# / WinUI / .NET 10

## Summary
The Monitors page used a joined string signature to decide whether monitor overview rows needed rebuilding. This pass replaced that repeated string allocation path with an AppState.MonitorsVersion counter that advances only when live monitor rows or overrides change.

## Baseline Metrics
| Metric | Value |
|--------|-------|
| Dedicated benchmark harness | Not present |
| Affected refresh paths | 1 page |
| Monitor rows scanned per refresh before | All live monitors |
| Signature allocation before | string.Join plus interpolated fragments per refresh |
| Correctness baseline | Clean main at v0.1.51 |

## Hotspots
1. Pages/MonitorsPage.xaml.cs: MonitorListSignature built a joined string from every monitor row during refresh.
2. Services/AppState.cs: the actual monitor mutation points were already centralized enough to expose a cheap version counter.

## Findings
- What: Monitor overview freshness checks rebuilt a string signature during each Monitors page refresh.
- Why: Runtime/app-state notifications can refresh the page while monitor rows have not changed; the freshness check still scanned and allocated.
- Fix: Add AppState.MonitorsVersion, increment it on live monitor collection replacement and live override changes, then compare that integer in MonitorsPage.

## Optimizations Applied
| Change | Before | After | Improvement |
|--------|--------|-------|-------------|
| Monitor overview freshness checks | Rebuild joined monitor signature strings per refresh | Compare MonitorsVersion integer | Removes repeated monitor scan/signature allocation when monitor rows are unchanged |

## After Metrics
| Metric | Value |
|--------|-------|
| Dedicated benchmark harness | Not present |
| Monitor rows scanned per unchanged refresh after | 0 |
| Signature allocation after | None for unchanged refresh |
| Correctness validation | Release build, unit tests, workbook parser, live launcher |

## Recommendations
Continue looking for refresh paths that rebuild ItemsSource rows or derived text on every state notification. Consider a small benchmark or diagnostic counter harness if the remaining opportunities become less obvious than allocation-removal passes.
