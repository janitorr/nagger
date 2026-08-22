## Why

The MCP `list_one_shot_tasks` and `list_recurring_tasks` tools fail on every call for clients that validate `structuredContent` against the MCP spec. Both tools return a top-level JSON array, but the spec requires `structuredContent` to be a JSON object. Strict clients reject the result with a `ValidationError` (`dict_type`) before the caller can read the list. The other tools are unaffected because they return a single object.

## What Changes

- Wrap both list results in an object (`{ "tasks": [...] }`) so `structuredContent` validates as a JSON object.
- Point each list tool's `OutputSchemaType` at a wrapper record so the advertised output schema matches the returned shape.
- Clarify the `list_one_shot_tasks` spec and add a spec requirement for the previously undocumented `list_recurring_tasks` tool.

## Capabilities

### New Capabilities

### Modified Capabilities

- `mcp-task-api`: Correct the `list_one_shot_tasks` structured-content shape and document the `list_recurring_tasks` tool.

## Impact

- Only `src/Nagger.Host/Mcp/McpTaskTools.cs` and its MCP integration tests change; Core, the REST endpoints, and the SQLite mapping are unaffected.