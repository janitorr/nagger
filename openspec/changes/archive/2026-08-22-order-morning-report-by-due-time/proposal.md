## Why

The morning report returns its `items` array in ascending task id order (all one-shot tasks, then all recurring instances, each id-ordered). For a rundown whose purpose is "what do I do next," id order is backwards — items due tomorrow can land behind items due next week. This ordering is currently unspecified, so there is no documented contract breaking; it is an implementation detail that happens to be id-ordered.

## What Changes

- Order the `items` array chronologically by due date/time: overdue first, then due-today, then upcoming, earliest due first.
- Within a single due date, order by due time (10:15 sorts before 13:00).
- Sort by `dueAt` ascending overall; `dueAt` is a `DateTimeOffset` compared by UTC instant, which yields the grouping above for free.
- Overdue items sort oldest (most overdue) first, consistent with "earliest due first" applied across the overdue group.
- Items with an identical `dueAt` timestamp have unspecified relative order (the sort is stable, but no tiebreaker is a contract).
- `summary` counts are unaffected; `GET /reports/morning` remains read-only. The same ordering applies to the MCP `get_morning_report` tool, which shares the same handler.
- Document the ordering in `USAGE.md` and in the `morning-task-report` spec so it stops being unspecified.

## Capabilities

### New Capabilities

_None._

### Modified Capabilities

- `morning-task-report`: add a requirement that the report `items` array is ordered chronologically by due date/time.

## Impact

- `src/Nagger.Core/Tasks/MorningReport.cs` — sort the merged item list by `DueAt` before returning the report.
- `tests/Nagger.Core.Tests/TaskFeatureTests.cs` — add a Core test asserting chronological ordering (including one-shot/recurring interleaving).
- `tests/Nagger.Host.Tests/ApiTests.cs` — add a Host test asserting the JSON `items` array order.
- `USAGE.md` — document the ordering and update the example, which is currently id-ordered.
- `openspec/specs/morning-task-report/spec.md` — the ordering requirement (synced via the delta spec).
