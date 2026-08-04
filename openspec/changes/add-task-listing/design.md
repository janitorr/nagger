## Context

One-shot task lifecycle operations are addressed by durable numeric ID. An LLM operating without prior conversational context cannot obtain an ID for an upcoming or paused task: the only current collection read, the morning report, intentionally includes only active due-today and overdue items. The service is a local, single-user application with a small task volume.

## Goals / Non-Goals

**Goals:**
- Provide a zero-input, read-only inventory of open one-shot tasks for REST and MCP clients.
- Return durable IDs and the established full task representation so clients can safely select a subsequent lifecycle action.
- Keep Core as the owner of query behavior and expose persistence through its existing port boundary.

**Non-Goals:**
- Title, date, or status filters.
- Pagination, cursors, or result limits.
- Lookup of a single task by ID.
- Listing completed or cancelled task history.
- Changes to morning-report behavior or task persistence schema.

## Decisions

### Define open tasks as active and paused

The list returns active and paused tasks, ordered by ascending durable ID. Both are actionable states: active tasks can be completed, paused, or cancelled, and paused tasks can be resumed or cancelled. Terminal tasks are excluded to keep the default LLM context concise and to focus discovery on actions that remain possible.

Alternative: list only active tasks. Rejected because a user cannot discover a paused task to resume or cancel. Alternative: list every task. Rejected because retained terminal records grow the default response without helping routine task management.

### Add a Core query and open-task store operation

Add a `ListOpenOneShotTasksQuery` handled in `Nagger.Core.Tasks`, backed by a new `ITaskStore` operation. The SQLite adapter filters the existing status column to active and paused and orders by ID. This retains the current vertical-slice architecture and keeps EF Core out of Core.

Alternative: have Host query `NaggerDbContext` directly. Rejected because it bypasses the Core port boundary and creates divergent REST/MCP behavior.

### Reuse established response shapes on two read transports

Map `GET /tasks/one-shot` to the Core query and return an array of existing `TaskResponse` values. Add a read-only, zero-argument MCP `list_one_shot_tasks` tool returning an array of existing `McpTaskResponse` values. The MCP tool description will state that returned IDs are the identifiers required by lifecycle tools.

Alternative: make discovery MCP-only. Rejected because REST and MCP currently expose the same task operations and the REST endpoint makes the capability independently usable and testable.

## Risks / Trade-offs

- [Open-task count eventually exceeds useful LLM context] -> The initial local single-user scope accepts an unpaginated response; introduce filtering or pagination only when observed task volume requires it.
- [Similar task titles cause an incorrect lifecycle action] -> Return IDs, status, due timestamp, and full task fields so the LLM can disambiguate or ask the user before acting.
- [Future task types require different list semantics] -> The query and route are explicitly one-shot-task scoped, leaving room for type-specific behavior later.
