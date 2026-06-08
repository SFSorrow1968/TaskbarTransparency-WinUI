# Performance Report: Monitor Override Lookup Loop
Date: 2026-06-08
Mode: optimize
Language: C# / WinUI / .NET 10

## Summary
The per-monitor override lookup path still used a pre-check and LINQ grouping when unsynced monitor overrides existed. This pass replaces that with a single loop that only allocates the dictionary once an unsynced monitor is found.

## Baseline Metrics
| Metric | Value |
|--------|-------|
| Dedicated benchmark harness | Not present |
| Affected apply path | TaskbarAppearanceService.BuildMonitorOverrideLookup |
| Lookup before | Any + Where + GroupBy + ToDictionary |
| Correctness baseline | Clean main at v0.1.58 |

## Hotspots
1. Services/TaskbarAppearanceService.cs: monitor override lookup construction had two scans and grouping overhead.

## Findings
- What: The unsynced monitor path used multiple LINQ steps to preserve one override per device.
- Why: Per-monitor overrides are part of the apply path, and this logic can be expressed directly while preserving first-entry semantics.
- Fix: Build the lookup in one loop with a case-insensitive dictionary and TryAdd.

## Optimizations Applied
| Change | Before | After | Improvement |
|--------|--------|-------|-------------|
| Monitor override lookup | Any + Where + GroupBy + ToDictionary | Single loop, lazy dictionary allocation | Less managed allocation and one pass over monitors |

## After Metrics
| Metric | Value |
|--------|-------|
| Dedicated benchmark harness | Not present |
| Fully synced monitors | No dictionary |
| Unsynced monitors | One loop |
| First override per device | Preserved by test |
| Correctness validation | Release build, unit tests, workbook parser, live launcher |

## Recommendations
Use Diagnostics counters for further apply-path decisions. The remaining simple allocation paths are now mostly outside per-monitor lookup and alpha/cached-state bookkeeping.
