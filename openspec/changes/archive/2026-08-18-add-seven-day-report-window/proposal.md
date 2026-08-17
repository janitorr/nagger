## Why

The morning report currently counts every future active task as upcoming but only provides item detail for due-today and overdue work. This does not give the assistant a focused planning view of obligations that are approaching, while one-shot tasks need to stay visible after their due date until the user explicitly completes them.

## What Changes

- Change the morning report visibility window so active one-shot tasks appear as detailed upcoming items only when their local due date is within the inclusive seven calendar days following the requested report date.
- Keep active due-today and overdue tasks visible as detailed items; overdue tasks remain visible until completed, paused, or cancelled.
- Add `daysUntilDue` to upcoming report items so report consumers can prioritize approaching work without independently calculating dates.
- **BREAKING** Bump the morning report schema version because the report's item selection and item contract change.
- Preserve `reminderPolicy` as persisted task metadata for future reminder-delivery and recurring-task work; it does not affect current report selection.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `morning-task-report`: Include only a seven-day upcoming planning window in detailed report output and expose the number of days until each upcoming task is due.

## Impact

- Updates the Core morning-report query and its deterministic date classification.
- Changes REST and MCP morning-report response payloads and report schema version.
- Updates Core, REST integration, and MCP integration tests, plus API usage documentation and product brief reporting guidance.
