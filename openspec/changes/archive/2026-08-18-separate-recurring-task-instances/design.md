## Context

Nagger uses vertical slices in `src/Nagger.Core/Tasks/` with ports in `Ports.cs`, implemented by SQLite adapters in `src/Nagger.Host/Infrastructure/`. Recurring tasks today store their generated instances as rows in `one_shot_tasks` via a nullable `RecurringTaskId` column, so one-shot and recurring-instance data share one table and one ID space. The one-shot complete handler detects `RecurringTaskId` and spawns the next instance, and `/tasks/recurring/{id}/complete` resolves `{id}` as an instance id while the sibling pause/resume/cancel endpoints resolve it as a template id. See proposal.md for motivation.

## Goals / Non-Goals

**Goals:**
- Give recurring instances their own table, store, and ID space, fully decoupled from one-shot tasks.
- Make the recurring template the aggregate root: every `/tasks/recurring/{id}/...` endpoint addresses a template id.
- Restore the one-shot complete handler to depend only on `ITaskStore` and `IClock`.
- Keep the morning report reporting recurring obligations, disambiguated by a `type` field.

**Non-Goals:**
- No template editing, listing of individual instances, or new instance endpoints.
- No reminder delivery or time-of-day scheduling.
- No data migration of existing rows (no production data exists).

## Decisions

### Decision: Dedicated recurring instance slice
Introduce `RecurringTaskInstance` (id, recurring task id, title, dueAt, reminderPolicy, status, createdAt, updatedAt, completedAt?, cancelledAt?) and a `IRecurringTaskInstanceStore` port with `AddAsync`, `GetByIdAsync`, `UpdateAsync`, `GetActiveAsync`, and `GetByTemplateIdAsync`. Host implements it in `SqliteRecurringTaskInstanceStore` backed by a new `recurring_task_instances` table.

The instance keeps a `RecurringTaskId` field linking to its template; that field now lives inside the recurring slice rather than on the one-shot `TaskItem`.

- Alternative (rejected): keep the nullable FK on `one_shot_tasks`. Mixes two concepts, forces one-shot logic to know recurrence.

### Decision: Template is the aggregate root
`POST /tasks/recurring/{id}/complete` resolves `{id}` as the template id. The handler loads the template (`RecurringTaskNotFoundException` → 404 when absent), finds its current `active` instance, completes it, and creates the next instance from `RecurrenceCalculator`. If the template has no active instance, it returns a structured validation error.

Pause/resume/cancel keep their current observable behavior but read/write instances through `IRecurringTaskInstanceStore.GetByTemplateIdAsync` instead of `ITaskStore.GetByRecurringTaskIdAsync`.

- Alternative (rejected): complete by instance id. Requires callers to track instance ids separately and makes the same URL segment mean two different things.

### Decision: New status enum for instances
Use a new `RecurringTaskInstanceStatus` (`active`, `paused`, `done`, `cancelled`) with its own contract-value mapping, rather than reusing `OneShotTaskStatus`. Instances and one-shot tasks are separate concepts; reusing the enum would reintroduce the coupling this change removes.

### Decision: Morning report aggregates two stores and adds a discriminator
`MorningReportItem` gains a `Type` field (`one-shot` or `recurring`). `MorningReportHandler` reads active one-shot tasks from `ITaskStore` and active recurring instances from `IRecurringTaskInstanceStore`, then classifies both through the existing due-state logic. Recurring items emit the template id as `id` and the instance's due timestamp as `dueAt`. `schemaVersion` bumps from `"2"` to `"3"`.

- Alternative (rejected): drop recurring from the report. Would hide recurring obligations from the digest.
- Alternative (rejected): keep reporting recurring instances under their instance id. Ambiguous for callers and inconsistent with template-id completion.

### Decision: Migration with no data movement
A single EF migration creates `recurring_task_instances` and drops the `RecurringTaskId` column from `one_shot_tasks`. Because there is no production data, no row relocation is needed. Migrations run automatically on Host startup.

## Risks / Trade-offs

- [ID ambiguity across the three ID spaces] → The report's `type` field and MCP tool descriptions (each `id` names its source) disambiguate for callers.
- [Delta overlap with `add-seven-day-report-window` on `morning-task-report`] → Sync and archive that change before applying this one so the canonical spec baseline includes schemaVersion `"2"`.
- [Overdue instances accumulate while a recurring task stays undone] → Unchanged behavior; acceptable per existing design.

## Migration Plan

1. Add the new instance record, port, store, and EF entity/mapping.
2. Generate the migration (new table + drop column); verify it applies cleanly on a fresh database.
3. Rewire handlers and endpoints; update MCP tool descriptions.
4. Update Core/Host/MCP tests and run the full suite plus Core mutation testing.

Rollback: revert to the previous schema (restore the column, drop the table); no data to relocate.
