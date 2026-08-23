## Context

`reminderPolicy` is a required input on both create operations and is echoed in every task/template/instance representation and report item, but nothing ever acts on it — there is no delivery path and none is planned (see proposal.md — Why). The report is the only delivery channel. `LastReminderAt` is an orphaned column on one-shot tasks that is mapped but never written or read. EF migrations are applied automatically on Host startup.

## Goals / Non-Goals

**Goals:**
- Purge the reminder-delivery model entirely: the enum, the `reminderPolicy` fields on all three domain records, and `LastReminderAt`.
- Slim the REST, MCP, and report contracts so no `reminderPolicy` appears anywhere.
- Bump the report `schemaVersion` to reflect a field removal.
- Emit an EF migration that drops the affected columns from existing databases.

**Non-Goals:**
- No replacement field (e.g. a `nagUntilDone` flag) — nag-until-done is already emergent from `overdue`/`daysOverdue` plus status.
- No change to the recurring MCP tools' spec surface beyond dropping `reminderPolicy`; the recurring MCP tools are not yet in `mcp-task-api` and that gap is left as-is.
- No report contract redesign beyond the version bump.

## Decisions

**Remove `ReminderPolicy` rather than defaulting or keeping it unused.**
A dead enum is worse than none: keeping it would preserve a required no-op field and the LLM confusion it causes. Alternative rejected: default the value and stop requiring it — still leaves an inert field and stale contract surface. Removing the type lets the compiler find every dangling reference.

**Bump report `schemaVersion` `"3"` → `"4"`.**
Per project convention, removing data is a breaking change and warrants a version bump; adding a field would not. Consumers keyed on `"3"` must be updated. The constant is a literal in `MorningReport.cs`; change it in place.

**Drop the columns via a new EF migration, not a database rebuild.**
Migrations auto-apply on startup, so existing local databases keep their rows. SQLite/EF can `DROP COLUMN` a `NOT NULL` column; since we are deleting columns there is no backfill. One migration drops `ReminderPolicy` from tasks, templates, and instances, plus `LastReminderAt` from tasks. Alternative rejected: deleting `nagger.db` and letting it regenerate — lossy and unnecessary.

**Bundle `LastReminderAt` removal here.**
It is part of the same vestigial delivery model (never written, never read) and would otherwise be the last orphan of it.

**Trim "reminder state/timestamps" from the report read-only guarantees.**
The specs and MCP description reference a reminder state that no longer exists. The read-only guarantee is reduced to task state and timestamps, matching what the system actually holds.

**Drop `reminderPolicy` from the recurring MCP tools too, even though they lack spec coverage.**
`create_recurring_task` accepts it and the recurring tool responses return it; removing it keeps the whole MCP surface consistent with the same "no policy" model.

## Risks / Trade-offs

- **Breaking change for the assistant consumer** → local, single-consumer, low blast radius; `USAGE.md`, `README.md`, and `docs/hermes-integration.md` (if it references the field) are updated in the same change.
- **Existing databases with `NOT NULL` `ReminderPolicy` columns** → the migration drops the columns; verify the migration applies cleanly against a pre-change `nagger.db`.
- **MCP clients cache tool schemas** → clients must reconnect to pick up the slimmer tool signatures; no runtime config change.
- **Report consumers keyed on `"3"`** → updated to expect `"4"` and the absent `reminderPolicy`.

## Migration Plan

1. Update Core: delete `ReminderPolicy.cs`, remove the property from the three domain records and the two create commands.
2. Update Host adapters (endpoints, MCP tools, stores, `NaggerDbContext`).
3. Generate the EF migration dropping `ReminderPolicy` (tasks/templates/instances) and `LastReminderAt` (tasks).
4. Update the report constant to `"4"` and drop `reminderPolicy` from report items.
5. Update tests and docs.
6. Build, run Core + Host tests, run Core mutation testing.

Rollback: revert the commit; the migration is forward-only column drops, so a rollback means restoring the prior schema via the prior migration snapshot (or a fresh local DB).

## Open Questions

None that change the specs, approach, or task breakdown.
