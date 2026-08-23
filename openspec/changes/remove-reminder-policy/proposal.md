## Why

Nagger has no reminder-delivery mechanism and never will: the morning report is the only delivery channel, read by an assistant over REST or MCP. The required `reminderPolicy` field (`none`, `once`, `weekly-until-done`) encodes a delivery/escalation model that will never exist, so it records intent with zero runtime behavior — a required answer to a question nothing asks. Removing it simplifies the create contracts, the task and report representations, and the MCP tool surface, and eliminates the primary source of LLM tool-argument confusion.

## What Changes

- Remove `reminderPolicy` from one-shot task creation (REST + MCP): no longer required, no longer validated, no longer returned. **BREAKING**
- Remove `reminderPolicy` from recurring task creation (REST + MCP) and from recurring template and instance representations. **BREAKING**
- Remove `reminderPolicy` from morning report item fields and bump `schemaVersion` from `"3"` to `"4"`. **BREAKING**
- Remove the internal `ReminderPolicy` domain enum and the `ReminderPolicy` property from `TaskItem`, `RecurringTaskTemplate`, and `RecurringTaskInstance`, plus the orphaned `LastReminderAt` field/column that was never written or read.
- Remove the now-obsolete "reminder delivery" planned items from the product brief and design docs.

Nag-until-done behavior is unchanged: it is emergent from the report's `overdue`/`daysOverdue` classification combined with task status, not from any policy.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `one-shot-task-creation`: `reminderPolicy` is no longer a required, validated, or returned field.
- `recurring-task-creation`: `reminderPolicy` is no longer a required or validated field.
- `morning-task-report`: `reminderPolicy` is removed from item detail and `schemaVersion` becomes `"4"`.
- `mcp-task-api`: the `create_one_shot_task` tool no longer accepts `reminderPolicy`.
- `one-shot-task-listing`: the listed task representation no longer includes a reminder policy.

## Impact

- **Core**: `ReminderPolicy.cs`, `TaskItem.cs`, `RecurringTaskTemplate.cs`, `RecurringTaskInstance.cs`, `CreateOneShotTask.cs`, `CreateRecurringTask.cs`.
- **Host**: `TaskEndpoints`, `RecurringTaskEndpoints`, `ReportEndpoints`, `McpTaskTools`, the SQLite stores, `NaggerDbContext`, and a new EF migration dropping the `ReminderPolicy` and `LastReminderAt` columns.
- **Docs**: `USAGE.md`, `README.md`, `docs/product-brief.md`, `docs/product-design.md`.
- **Tests**: `Nagger.Core.Tests` and `Nagger.Host.Tests` fixture payloads and assertions.
- **Contract break**: REST/MCP create payloads and task/report representations lose `reminderPolicy`; report `schemaVersion` changes `"3"` → `"4"`. Local, single-user, single-consumer — low blast radius.
