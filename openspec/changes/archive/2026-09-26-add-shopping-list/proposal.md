# Proposal

## Why

Users tell their personal assistant they're running low on household goods ("I'm running out of milk, yogurt, oats") and want those replenishments captured and surfaced on the morning report. Today Nagger only models dated reminders (one-shot and recurring tasks), so there is no way to record a dateless "need to buy" item.

## What Changes

- Add a name-keyed shopping item: a nonempty name, no due date, and no lifecycle states beyond "on the list" or "removed".
- Add REST endpoints to add (idempotent by name), remove (idempotent), and list shopping items.
- Add MCP tools `add_shopping_item`, `remove_shopping_item`, and `list_shopping_items`.
- Extend the morning report with a `shopping` section listing open items, and bump `schemaVersion` from `"4"` to `"5"`.
- Persist shopping items in a new SQLite store with a migration.

## Capabilities

### New Capabilities

- `shopping-list`: the shopping-item domain — name-keyed identity, idempotent add, idempotent remove, and listing — plus its REST endpoints.

### Modified Capabilities

- `morning-task-report`: the report gains a `shopping` section and its `schemaVersion` becomes `"5"`.
- `mcp-task-api`: new `add_shopping_item`, `remove_shopping_item`, and `list_shopping_items` tools, and `get_morning_report` now returns the shopping section.

## Impact

- `src/Nagger.Core/Tasks/`: new vertical slices (add, remove, and list shopping items), a `ShoppingItem` domain entity, and a new store port.
- `src/Nagger.Host/Infrastructure/`: a new SQLite adapter and migration.
- `src/Nagger.Host/Api/`: shopping endpoints.
- `src/Nagger.Host/Mcp/`: three new tools.
- `MorningReport` handler reads a third reader and emits the `shopping` section (the report remains read-only).
- `USAGE.md`: document the new endpoints, tools, and report section.
