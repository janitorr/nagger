## Why

MCP lifecycle tools require a task ID, but an LLM that has not seen a create response or morning report cannot discover one. The morning report is intentionally not a task inventory: it omits upcoming and paused tasks.

## What Changes

- Add a read-only operation that lists all open one-shot tasks, meaning active and paused tasks, with their durable IDs and normal task fields.
- Expose the operation through `GET /tasks/one-shot` and the MCP `list_one_shot_tasks` tool.
- Keep the operation intentionally unfiltered and unpaginated for the current single-user, low-volume use case.

## Capabilities

### New Capabilities
- `one-shot-task-listing`: List active and paused one-shot tasks so clients can resolve task IDs before lifecycle actions.

### Modified Capabilities
- `mcp-task-api`: Advertise and provide the read-only MCP task-listing tool.

## Impact

- Affected Core task store port and query feature.
- Affected SQLite task-store adapter, REST task endpoints, and MCP task tools.
- New Core and Host integration coverage for list behavior and MCP tool discovery/calls.
