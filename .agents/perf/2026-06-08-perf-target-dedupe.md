# Performance Report: Taskbar Target De-Dupe
Date: 2026-06-08
Mode: optimize
Language: C# / WinUI / .NET 10

## Summary
The taskbar apply path used LINQ grouping to de-duplicate taskbar targets by handle before each apply. This pass replaces that grouping with a single loop that preserves the first target per handle and pre-sizes the per-apply target dictionary.

## Baseline Metrics
| Metric | Value |
|--------|-------|
| Dedicated benchmark harness | Not present |
| Affected apply path | TaskbarAppearanceService.Apply |
| Target de-dupe before | GroupBy + Select + ToArray |
| Dictionary sizing before | Default |
| Correctness baseline | Clean main at v0.1.56 |

## Hotspots
1. Services/TaskbarAppearanceService.cs: target de-dupe allocated grouping machinery during every apply.

## Findings
- What: Apply used a LINQ grouping pipeline to remove duplicate taskbar handles.
- Why: The apply path is frequently invoked and only needs to preserve the first target per handle.
- Fix: Add a direct DistinctByHandle loop and pre-size alphaTargets from the de-duped target count.

## Optimizations Applied
| Change | Before | After | Improvement |
|--------|--------|-------|-------------|
| Target de-dupe | LINQ grouping pipeline | Single loop with HashSet and pre-sized List | Less managed allocation before native apply work |
| Alpha target dictionary | Default capacity | Capacity set to target count | Fewer dictionary resizes in apply path |

## After Metrics
| Metric | Value |
|--------|-------|
| Dedicated benchmark harness | Not present |
| Target de-dupe after | Single loop |
| First-target semantics | Preserved by test |
| Correctness validation | Release build, unit tests, workbook parser, live launcher |

## Recommendations
Use Diagnostics counters and, if needed, a small benchmark harness before further apply-path changes. Remaining costs are likely native calls and taskbar enumeration rather than simple LINQ allocation.
