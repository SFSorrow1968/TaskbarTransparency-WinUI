# Performance Report: Layered Alpha Change Loop
Date: 2026-06-08
Mode: optimize
Language: C# / WinUI / .NET 10

## Summary
The layered alpha apply path used a LINQ projection/filter/dictionary chain to collect changed taskbar alpha targets. This pass replaced it with a single pre-sized loop that also counts skipped targets for diagnostics.

## Baseline Metrics
| Metric | Value |
|--------|-------|
| Dedicated benchmark harness | Not present |
| Affected apply path | TaskbarAppearanceService.AnimateTaskbarAlpha |
| Collection before | Select + Where + ToDictionary |
| Diagnostics skip count | Derived from dictionary count |
| Correctness baseline | Clean main at v0.1.55 |

## Hotspots
1. Services/TaskbarAppearanceService.cs: alpha target collection did repeated iterator/projection work on every apply.

## Findings
- What: The apply path allocated LINQ iterator/projection machinery while also needing a skip count.
- Why: Taskbar apply runs for startup, profile changes, runtime sensors, and manual reapply.
- Fix: Use one pre-sized dictionary and a direct loop to convert opacity, check the cached alpha, add changed targets, and count skipped targets.

## Optimizations Applied
| Change | Before | After | Improvement |
|--------|--------|-------|-------------|
| Alpha target collection | LINQ projection/filter/dictionary chain | Single pre-sized loop | Less managed allocation and one straightforward pass |

## After Metrics
| Metric | Value |
|--------|-------|
| Dedicated benchmark harness | Not present |
| Collection after | Single loop |
| Diagnostics skip count | Preserved |
| Correctness validation | Release build, unit tests, workbook parser, live launcher |

## Recommendations
Use the Diagnostics counters to decide whether animation startup or native call avoidance is the next real bottleneck before changing more apply logic.
