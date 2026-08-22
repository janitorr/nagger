## Context

`MorningReportHandler.Handle` in `src/Nagger.Core/Tasks/MorningReport.cs` builds the report `items` list by iterating `ITaskStore.GetActiveAsync()` (one-shot tasks) and then `IRecurringTaskInstanceStore.GetActiveAsync()` (recurring instances), appending each classified item in store order. Both SQLite stores return rows ordered by ascending `Id`, so the report comes back as "all one-shot by id, then all recurring by id" with no chronological ordering. The two stores are separate tables, so the merge happens only in the handler. See proposal.md for motivation.

## Goals / Non-Goals

**Goals:**

- Produce deterministic chronological `items` ordering independent of store ordering.
- Apply the same ordering to both `GET /reports/morning` and the MCP `get_morning_report` tool (they share the handler).
- Keep `summary` counts and the read-only invariant unchanged.

**Non-Goals:**

- No persistence or schema changes; no ordering push-down into the SQLite queries.
- No tiebreaker contract for identical `dueAt` timestamps.
- No changes to due-state classification, `daysOverdue`/`daysUntilDue`, or the seven-day visibility window.

## Decisions

1. **Sort in the handler, not the stores.** One-shot and recurring items come from two separate stores/tables, so a single SQL `ORDER BY dueAt` cannot span both. The handler already owns report assembly and Core tests use in-memory stores with arbitrary order, so sorting there makes ordering a store-independent, testable contract.
   - *Alternative considered:* add `ORDER BY dueAt` to each store query — still leaves two sub-lists to interleave and couples ordering to the persistence layer. Rejected.

2. **Single sort key: `DueAt` ascending.** `DueAt` is a `DateTimeOffset`, compared by UTC instant. Due-state classification is monotonic in that instant, so ascending order yields overdue → due-today → upcoming automatically, and within-day time order falls out with no explicit grouping.
   - *Alternative considered:* group by `dueState` then sort each group — more code for the same result. Rejected.

3. **No tiebreaker.** Identical `dueAt` timestamps are rare, and `OrderBy` is stable so relative order remains deterministic (one-shot before recurring, id order within type). Adding `ThenBy(x => x.Id)` would be arbitrary because one-shot task ids and recurring template ids are separate id spaces. Documented as unspecified in the spec.

4. **No timezone conversion before sorting.** The raw stored `dueAt` is sorted directly by its UTC instant. Due-state classification is monotonic in that instant, so ascending order yields overdue → due-today → upcoming and within-day time order with no explicit grouping. Local-time order can differ from instant order only for the ambiguous hour during a DST fall-back fold, which is negligible in practice and not contracted by the spec.

## Risks / Trade-offs

- [Most-overdue-first ordering could differ from the user's mental model] → it matches the issue's "earliest due first" starting point and is the natural catch-up read; reversing it is a one-line change if April prefers otherwise.
- [Identical-`dueAt` ties keep a type-grouped relative order] → negligible in practice and now explicitly unspecified rather than silently id-ordered.
- [A consumer could have been relying on the old id order] → none documented; the ordering was previously unspecified and is being documented now in `USAGE.md` and the spec.

## Migration Plan

None. The endpoint is read-only and no data or schema changes are involved.
