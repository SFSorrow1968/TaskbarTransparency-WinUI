# Performance Report: Apply Diagnostics Counters
Date: 2026-06-08
Mode: optimize
Language: C# / WinUI / .NET 10

## Summary
The obvious apply-path allocation and refresh churn work is complete enough that further optimization should be counter-led. This pass added lightweight apply diagnostics for native composition skips, layered alpha skips, monitor lookup use, animation starts, and target count.

## Baseline Metrics
| Metric | Value |
|--------|-------|
| Dedicated benchmark harness | Not present |
| Apply skip counters before | Not exposed |
| Diagnostics visibility before | Runtime state only |
| Correctness baseline | Clean main at v0.1.54 |

## Hotspots
1. Services/TaskbarAppearanceService.cs: native composition and layered alpha skip decisions were internal only.
2. Pages/DiagnosticsPage.xaml.cs: technical details had no apply-path skip counters.

## Findings
- What: Future optimization passes lacked live evidence for how much taskbar apply work was already skipped.
- Why: Without counters, further micro-optimization would lean on source inspection rather than observed native-call avoidance.
- Fix: Record the latest apply diagnostics and surface them in Diagnostics technical details.

## Optimizations Applied
| Change | Before | After | Improvement |
|--------|--------|-------|-------------|
| Apply-path measurement | No exposed counters | Latest apply counters visible in Diagnostics | Future optimization can target measured remaining work |

## After Metrics
| Metric | Value |
|--------|-------|
| Apply target count | Exposed |
| Composition applied/skipped | Exposed |
| Layered alpha queued/skipped | Exposed |
| Monitor lookup built/skipped | Exposed |
| Animation started/not started | Exposed |
| Correctness validation | Release build, unit tests, workbook parser, live launcher |

## Recommendations
Use the new Diagnostics apply counters during manual runtime flows before making additional apply-path micro-optimizations. The next code change should target counters that remain high under real use.
