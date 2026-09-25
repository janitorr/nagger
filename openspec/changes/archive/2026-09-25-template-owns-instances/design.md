# Design

## Context

Recurring writes are orchestrated in Core handlers that call two store ports (`IRecurringTaskTemplateStore`, `IRecurringTaskInstanceStore`) in sequence. Both Host stores share one scoped `NaggerDbContext`, but each calls `SaveChangesAsync` independently; a Host-level `TransactionBehavior` (`where TMessage : ICommand`) wraps dispatch to make them all-or-nothing. Core must not reference EF or ASP.NET (AGENTS.md), so any persistence boundary lives in Host. See proposal.md - Why for motivation.

## Goals / Non-Goals

**Goals:**

- Make `RecurringTaskTemplate` the aggregate root owning its open instances; one aggregate = one `SaveChanges`.
- Delete `TransactionBehavior`; atomicity becomes a property of the model.
- Keep create/complete/pause/resume/cancel observable behavior identical.
- Keep the morning-report read path querying instances directly (CQRS).

**Non-Goals:**

- No API/HTTP/MCP contract changes (`skip_specs`).
- No change to one-shot task writes (already single-save per handler).
- No schema/data migration; `done`/`cancelled` rows are retained, not relocated or purged.
- No change to instance retention semantics.

## Decisions

**1. Aggregate owns open instances only, hydrated via a filtered navigation.**
`RecurringTaskTemplate` gains `IReadOnlyList<RecurringTaskInstance> Instances`. `GetByIdAsync` hydrates only `active`/`paused` instances; `done`/`cancelled` rows stay in the DB but are never hydrated. Terminal instances are immutable facts, so they sit outside the consistency boundary.
*Alternative (rejected)*: own all instances (the issue's original wording). Every lifecycle write would drag unread history into memory; open-only keeps the aggregate O(1) while the rows remain for future audit.

**2. Reconcile-by-id in a single save.**
`UpdateAsync(template)` loads the template entity (tracked) with its instances, then for each instance in the aggregate list: `id > 0` → update mutable fields, `id == 0` → add; never touch rows not in the list. One `SaveChangesAsync` persists template + instances. Instances are never deleted.
*Alternative (rejected)*: EF graph diff via attach/state — more implicit, harder to reason about with immutable `with` records.

**3. `UpdateAsync` returns the persisted aggregate.**
Needed so the complete handler can report `nextInstance`'s real id. Under decision 1, re-reading `GetByIdAsync` after the save would not return the just-completed (now `done`) instance, losing the completed instance from the response.
*Alternative (rejected)*: void `UpdateAsync` + re-read — broken by decision 1.

**4. `Complete` computes the next due date inside the domain.**
`Complete(DateTimeOffset now, TimeZoneInfo localTimeZone)` marks the active instance done and mints the next from `RecurrenceCalculator` + the local completion date. Keeps the recurrence rule's only consumer in the model. `TimeZoneInfo` (inert) is passed rather than `TimeProvider` so the aggregate cannot re-read the clock and break the single-instant invariant (#54).
*Alternative (rejected)*: handler precomputes `nextDueAt` and passes it in — splits the rule between handler and model, re-externalizing the invariant this change internalizes.

**5. Handlers extract results by role.**
After `UpdateAsync`, complete extracts `completedInstance` = the `done` instance and `nextInstance` = the `active` instance from the persisted aggregate. Unambiguous under decision 1 (exactly one of each post-complete).
*Alternative (rejected)*: `Complete` returns `(template, completed, next)` — the `next` still has `Id: 0` until persistence, so the handler must still re-derive it by role; adds a second mechanism without removing the first.

**6. Instance port shrinks to a reader.**
`IRecurringTaskInstanceStore` → `IRecurringTaskInstanceReader` with only `GetActiveAsync` (the report's only need). The write and orchestration-read methods are absorbed into the aggregate store. Renamed because a "store" that cannot write is misleading. `RecurringTaskInstance` keeps `RecurringTaskId` because the report uses it as the item id.

**7. List query loads no instances.**
`GetAllAsync` returns templates only (empty instance list); the list response exposes template identity and scalars, never instances.
*Alternative (rejected)*: project a separate read model — speculative machinery with no caller demanding it; revisit only if history ever becomes a real query problem.

**8. Atomicity is structural; no fault-injection test replaces the transaction tests.**
The two Host transaction tests and `FailingRecurringTaskInstanceStore` are deleted. The guarantee is one `SaveChangesAsync` per handler plus the absence of a second write.
*Alternative (rejected)*: a `SaveChangesAsync`-counting spy — couples the test to implementation rather than behavior.

## Risks / Trade-offs

- [No DB-level "one open instance" constraint] → Role extraction assumes at most one open instance. The state machine guarantees it and no API path violates it; a partial unique index is a future option, not part of this change.
- [EF model snapshot without a migration] → Adding the `Instances` navigation reuses the existing `RecurringTaskId` FK, so no schema migration; the snapshot must be regenerated and verified to not emit a spurious empty migration.
- [Mutation score] → Orchestration moves into Core, growing the mutable surface. Rewrite `RecurringTaskFeatureTests` against the aggregate; `dotnet stryker` gates at ≥75% (target 80%).
- [History rows never hydrated] → A future "past completions" feature has the data; today's aggregate stays lean. If history grows, it is a query (not model) concern.

## Migration Plan

No data or schema migration. Only the EF model snapshot changes (navigation added on the existing FK). Migrations still apply automatically on Host startup; nothing to relocate. Rollback = revert the model and store changes.

## Open Questions

None.
