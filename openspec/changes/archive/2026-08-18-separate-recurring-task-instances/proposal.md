## Why

The recurring-tasks feature stores generated instances inside the `one_shot_tasks` table through a nullable `RecurringTaskId` foreign key. That mixes two semantically distinct things into one store, forces the one-shot complete handler to know about recurrence (spawning the next instance), and leaves `/tasks/recurring/{id}/complete` resolving `{id}` as an instance id while `/tasks/recurring/{id}/pause|resume|cancel` resolve the same segment as a template id. The model should treat the recurring task as the aggregate root with its own instance store.

## What Changes

- Add a dedicated `recurring_task_instances` store (new table) and remove `RecurringTaskId` from one-shot tasks. No data migration is needed: there is no production data yet, so the migration only creates the new table and drops the column.
- Make the recurring task template the aggregate root: every `/tasks/recurring/{id}/...` endpoint resolves `{id}` as the template id. Complete finds the template's current active instance, marks it done, and creates the next instance. **BREAKING**: `POST /tasks/recurring/{id}/complete` previously accepted an instance id; it now accepts the template id.
- Strip recurrence logic from the one-shot complete handler so it depends only on `ITaskStore` and `IClock` again.
- Morning report: add a `type` discriminator (`one-shot` | `recurring`) to each report item and bump `schemaVersion` to `"3"`. Recurring tasks are reported under their template id. **BREAKING**: report items gain a required `type` field and a new major `schemaVersion`.
- Update MCP tool descriptions so `complete_recurring_task` takes the template id returned by `list_recurring_tasks`, not an instance id.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `one-shot-task-lifecycle`: remove the rule that completing a recurring-generated instance via `POST /tasks/{id}/complete` creates the next instance.
- `recurring-task-lifecycle`: `POST /tasks/recurring/{id}/complete` resolves `{id}` as the template id and operates on the template's current active instance.
- `recurring-task-creation`: the first generated instance is a recurring task instance in its own store, not a one-shot task.
- `morning-task-report`: report items carry a `type` field and recurring tasks are reported under their template id; `schemaVersion` becomes `"3"`.

## Impact

- Core: `TaskItem`, `Ports.cs`, `CreateRecurringTask`, `ManageOneShotTaskLifecycle`, `ManageRecurringTaskLifecycle`, `MorningReport`, plus a new recurring-instance record.
- Host: `NaggerDbContext`, an EF migration, `SqliteTaskStore`, `SqliteRecurringTaskTemplateStore`, a new `SqliteRecurringTaskInstanceStore`, `TaskEndpoints`, `RecurringTaskEndpoints`, `McpTaskTools`.
- Report contract: `schemaVersion` `"2"` → `"3"` (breaking change to item shape).
- Sequencing note: `morning-task-report` is also modified by the pending `add-seven-day-report-window` change; that change should be synced/archived before this one is applied to avoid overlapping deltas on the same spec.
