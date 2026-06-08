# Performance Report: Monitor Apply Lookup
Date: 2026-06-08
Mode: optimize
Language: C# / WinUI / .NET 10

## Summary
Taskbar applies built a per-monitor lookup dictionary even when all monitors were synced to the primary opacity. This pass skips the lookup allocation unless at least one monitor is unsynced.

## Baseline Metrics
| Metric | Value |
|--------|-------|
| Dedicated benchmark harness | Not present |
| Affected apply path | TaskbarAppearanceService.Apply |
| Lookup allocation before | Dictionary built whenever monitors were provided |
| Default synced monitor lookup need | None |
| Correctness baseline | Clean main at v0.1.53 |

## Hotspots
1. Services/TaskbarAppearanceService.cs: monitor lookup was built before iterating taskbar targets, even for fully synced monitor sets.

## Findings
- What: The apply path allocated and populated a dictionary that was unnecessary when every monitor follows primary opacity.
- Why: The common default state uses synced monitors, and applies can run from startup, manual reapply, profile changes, and runtime sensors.
- Fix: Build an override lookup only when at least one monitor has SyncWithPrimary=false.

## Optimizations Applied
| Change | Before | After | Improvement |
|--------|--------|-------|-------------|
| Monitor lookup during taskbar apply | Always build lookup for provided monitor collection | Build lookup only when unsynced overrides exist | Removes dictionary allocation/population from default synced apply path |

## After Metrics
| Metric | Value |
|--------|-------|
| Dedicated benchmark harness | Not present |
| Lookup allocation for all-synced monitors | None |
| Unsynced monitor behavior | Preserved |
| Correctness validation | Release build, unit tests, workbook parser, live launcher |

## Recommendations
Further apply-path optimization should be based on a small benchmark or live diagnostic counters, since the remaining native interop cost is likely dominated by Windows calls and taskbar enumeration rather than simple managed allocations.
