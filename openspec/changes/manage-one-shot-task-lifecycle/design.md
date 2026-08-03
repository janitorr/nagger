## Context

The initial slice persists active one-shot tasks and provides creation plus a read-only Morning Report. Its Core `TaskItem` has no lifecycle status or terminal timestamps, and `ITaskStore` only adds tasks or reads active tasks. The SQLite `one_shot_tasks` table has the same limitation. The product brief defines the lifecycle state machine, but the existing service cannot yet represent it.

This change adds only one-shot lifecycle behavior. Core remains independent of HTTP, EF Core, SQLite, configuration, and the system clock; Host remains the adapter and composition root.

## Goals / Non-Goals

**Goals:**
- Persist explicit one-shot statuses and completion/cancellation timestamps.
- Enforce valid lifecycle transitions in deterministic, clock-injected Core handlers.
- Expose the four lifecycle commands through the established Minimal API and return a consistent updated task representation.
- Preserve the existing read-only Morning Report behavior while excluding all non-active tasks.
- Establish the lifecycle-aware initial database schema before production data exists.

**Non-Goals:**
- Recurring tasks, task editing, reminder emission, `next_reminder_at`, or delivery idempotency.
- A task history/event table, soft-delete framework, authorization, or concurrency policy.
- Changes to Morning Report JSON shape, versioning, ordering, or upcoming-item policy.

## Decisions

### Represent the state machine in the Core task model

Add a task-status value to `TaskItem`, plus nullable completion and cancellation timestamps. Lifecycle handlers load a task, validate the requested transition against its current status, use `IClock.UtcNow`, and persist the resulting model. The Core will return a distinguishable missing-task result so the Host can map it to 404; invalid transitions continue to use the existing structured validation exception path and map to 400.

This keeps all state rules and timestamp generation testable without infrastructure. Putting transition checks in endpoints or the SQLite adapter would duplicate logic and couple the rules to HTTP or EF Core.

### Keep task persistence port minimal and lifecycle-specific

Extend `ITaskStore` with lookup by numeric id and persistence of a transitioned task, while retaining `GetActiveAsync` for report reads. The SQLite adapter will filter `GetActiveAsync` by persisted `active` status; Morning Report therefore stays a pure query over report-eligible tasks.

A generic repository or task event store is not introduced because only one aggregate and a small set of explicit transitions are needed. Filtering in the persistence adapter avoids requiring report code to understand inactive states while preserving the existing active-only port contract.

### Replace the disposable initial EF Core migration

Update the initial `one_shot_tasks` schema to include non-null `Status` plus nullable `CompletedAt` and `CancelledAt` columns, then regenerate the initial EF Core migration and model snapshot. New rows receive an `active` status from Core, and the mapping requires a persisted status.

No production database exists, so compatibility with the initial migration is unnecessary. Replacing it avoids carrying a transitional default and upgrade path for a schema that has not been released.

### Use consistent task command responses

Expand `TaskResponse` into the common representation returned by both creation and lifecycle commands: id, title, one-shot type, status, schedule, policy, creation/update timestamps, and nullable terminal timestamps. Lifecycle endpoints return `200 OK` because they return the changed resource. A missing id returns 404 rather than a validation error, while an existing id with an illegal command returns the established structured 400 error.

Returning `204 No Content` would force clients to make another request to observe deterministic state and offers no advantage for this local API. Treating missing resources as validation errors would conflate resource lookup with transition rules.

## Risks / Trade-offs

- [A developer retains a local database created by the discarded initial schema] -> Delete and recreate that disposable local database before running the reshaped initial migration.
- [Concurrent lifecycle commands overwrite each other] -> This local single-user service has no concurrency policy yet; retain the current last-write model and defer optimistic concurrency until a real consumer requires it.
- [Inactive tasks leak into reports] -> Filter active records in the SQLite adapter and cover all inactive statuses with Core and Host report tests.
- [Endpoints expose inconsistent task data] -> Centralize response construction in the existing task response mapper and verify creation plus each lifecycle response in integration tests.

## Migration Plan

1. Extend the Core task model, status conversion, persistence port, and lifecycle handlers with unit coverage.
2. Update the EF Core entity mapping and regenerate the initial migration and model snapshot with lifecycle fields.
3. Delete any disposable local database created by the prior initial schema, then verify Host startup creates the reshaped schema.
4. Implement SQLite lookup/update behavior and the four HTTP endpoint mappings with 404 handling.
5. Deploy normally; Host startup applies the initial migration before requests are served. Roll back by stopping the Host and deleting the disposable database before this first production release.

## Open Questions

None. The change uses the product brief's state machine and selects conventional 200, 400, and 404 response semantics for the otherwise unspecified endpoint outcomes.
