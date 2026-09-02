## Context

Recurring writes are orchestrated in Core handlers that call two store ports (`IRecurringTaskTemplateStore`, `IRecurringTaskInstanceStore`) in sequence. Both Host stores share the same scoped `NaggerDbContext`, and each calls `SaveChangesAsync` independently, so today there is no transaction spanning a handler. Core must not reference EF or ASP.NET (AGENTS.md), so any transaction boundary must live in Host. See proposal.md - Why for motivation.

## Goals / Non-Goals

**Goals:**

- Make every recurring write (create, complete, pause, resume, cancel) atomic.
- Make each recurring lifecycle handler write a single consistent instant.
- Make recurrence-unit persistence symmetric with the status enums.
- Add the FK and indexes the recurring hot paths rely on.

**Non-Goals:**

- No API contract changes (`skip_specs`).
- No change to one-shot write paths (single save, already atomic per handler).
- No upper bound on `recurrence.every` (separate concern).

## Decisions

**#43 — transaction via a Mediator pipeline behavior constrained to `ICommand`.**
The Mediator library distinguishes `ICommand<T>` (writes) from `IQuery<T>` (reads). A `TransactionBehavior<TMessage,TResponse> where TMessage : ICommand` opens a transaction on the scoped `NaggerDbContext`, runs `next()`, and commits/rolls back. This needs zero Core changes and automatically covers `CancelRecurringTaskHandler`'s N+1 saves. Chosen over a unit-of-work port (would touch the Core port surface and every write handler) and a combined store operation (narrows the port, duplicates orchestration). The behavior is registered scoped (needs the scoped DbContext). Reads are unaffected since queries do not implement `ICommand`.

**#43 test lives in Host, not Core.** Because the transaction boundary is Host-level, a Core fake-store test cannot observe rollback. The Host integration test uses real SQLite plus a store wrapper that throws on its second save, then asserts no template row survives a failed create and the completion rolls back when the next instance fails.

**#54 — capture `now` once at the top of each recurring handler.**
`CompleteRecurringTaskHandler` calls `GetUtcNow()` three times; the pause/resume/cancel handlers call it twice. Capture `var now = ...` once and reuse it for the template/instance transitions and the next instance's `CreatedAt`/`UpdatedAt`. The one-shot lifecycle spec already requires `updatedAt == completedAt`; this gives recurring instances the same guarantee.

**#44 — contract-value pair for recurrence units.**
Add `RecurrenceUnits.FromContractValue` mirroring the three status helpers (throw `ArgumentOutOfRangeException` on unknown input), write via `ToContractValue()`, read via `FromContractValue`. A migration rewrites existing `"Days"`/`"Weeks"`/`"Months"` rows to lowercase so no `.ToString()`/`Enum.Parse` remains on a persisted enum.

**#49 — FK with `Restrict` + targeted indexes.**
Configure the relationship and FK with `OnDelete(DeleteBehavior.Restrict)` (templates are never deleted, only cancelled). Add indexes on `RecurringTaskId` and on `Status` for both the instance and one-shot tables, since those are the columns the hot paths filter on. SQLite rebuilds the table to add the FK; verify the migration applies to a database with existing rows.

**Migrations: two, not one.** #44 (data rewrite) and #49 (FK/index) are separate concerns; separate migrations keep each reviewable and map 1:1 to their issue.

## Risks / Trade-offs

- [Transaction scope too broad] The behavior wraps every `ICommand`, including single-save one-shot writes → harmless: a transaction around one save is a no-op overhead. Accepted for simplicity.
- [SQLite DDL locks] Adding a FK/index requires a table rebuild that copies rows → existing rows must satisfy the constraint; the migration is verified against a populated DB in tests.
- [Mutation score] #54 touches Core handlers → verified with `dotnet stryker` ≥75%.
- [Advancing-clock test] #54 needs a `TimeProvider` that returns a different instant per call, since `FixedTimeProvider` would mask the bug → a small test double with a counter.

## Migration Plan

Migrations run automatically on Host startup (AGENTS.md). Local SQLite files upgrade in place. No rollback beyond the generated `Down()`; data is single-user and low-volume.

## Open Questions

None.
