# Performance Report: Stale Taskbar Cache Pruning
Date: 2026-06-08
Mode: optimize
Language: C# / WinUI / .NET 10

## Summary
Taskbar apply cache pruning still used projection, concatenation, filtering, and array materialization to remove stale handles. This pass prunes alpha and appearance caches directly against one live handle set built from the current target list.

## Baseline Metrics
| Metric | Value |
|--------|-------|
| Dedicated benchmark harness | Not present |
| Affected apply path | TaskbarAppearanceService.PruneStaleHandles |
| Prune before | Select + Concat + ToHashSet + Distinct + Where + ToArray |
| Correctness baseline | Clean main at v0.1.57 |

## Hotspots
1. Services/TaskbarAppearanceService.cs: stale cache pruning did extra managed allocation after every apply.

## Findings
- What: Pruning materialized a stale-handle array after scanning combined cache keys.
- Why: The apply path only needs to remove keys not present in the current target handles.
- Fix: Build one live handle set from taskbar targets and remove stale keys directly from each cache.

## Optimizations Applied
| Change | Before | After | Improvement |
|--------|--------|-------|-------------|
| Stale cache pruning | Projection/filter/array pipeline | Direct cache-key scans against live handle set | Less per-apply managed allocation |

## After Metrics
| Metric | Value |
|--------|-------|
| Dedicated benchmark harness | Not present |
| Stale array materialization | Removed |
| Correctness validation | Release build, unit tests, workbook parser, live launcher |

## Recommendations
Keep using Diagnostics counters for additional apply-path decisions. Remaining simple managed cleanup opportunities are now small; larger wins likely need instrumentation around taskbar enumeration and native interop.
