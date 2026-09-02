## Why

When a recurring task is created or completed, Nagger already writes the concrete instance row (the first instance on create, the next instance on complete), but both responses return only the template or the completed instance. The LLM agent that is Nagger's only caller ("April") cannot answer "when is the first one?" or "when's next?" without reading storage, because `list_recurring_tasks` returns templates only and the write-time instance never surfaces in any tool response.

## What Changes

- `POST /tasks/recurring` (and the `create_recurring_task` MCP tool) return the template **and** the newly created first instance. **BREAKING** — the response shape changes from a flat template object to an envelope.
- `POST /tasks/recurring/{id}/complete` (and the `complete_recurring_task` MCP tool) return the completed instance **and** the newly scheduled next instance. **BREAKING** — the response shape changes from a flat instance object to an envelope.
- Core handlers return result records that carry both values, capturing the instance id already assigned by the store's `AddAsync` (no extra query or write).
- MCP tool descriptions are updated so April is told explicitly that the response contains both instances and how to read the next/first due date.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `recurring-task-creation`: the create response returns a template plus its first instance instead of the template alone.
- `recurring-task-lifecycle`: the complete response returns the completed instance plus the next instance instead of the completed instance alone.
- `mcp-task-api`: `create_recurring_task` and `complete_recurring_task` tool responses carry the same envelope (these two tools are currently undocumented in the spec).

## Impact

- `src/Nagger.Core/Tasks/CreateRecurringTask.cs` — command result type and handler return.
- `src/Nagger.Core/Tasks/ManageRecurringTaskLifecycle.cs` — `CompleteRecurringTaskCommand` result type and handler return.
- `src/Nagger.Host/Api/RecurringTaskEndpoints.cs` — new envelope response records and endpoint mappings.
- `src/Nagger.Host/Mcp/McpTaskTools.cs` — new envelope output-schema records and updated tool descriptions.
- Tests: `tests/Nagger.Core.Tests/RecurringTaskFeatureTests.cs`, `tests/Nagger.Host.Tests/ApiTests.cs`, `tests/Nagger.Host.Tests/McpTests.cs`.
- Docs: `USAGE.md` recurring-task section.
- No storage, migration, or persistence changes.
