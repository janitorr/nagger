## Context

Every MCP tool funnels its result through `Run<T>` in `src/Nagger.Host/Mcp/McpTaskTools.cs`, which serializes the returned value into `StructuredContent`. The list tools return `McpTaskResponse[]` and `McpRecurringTemplateResponse[]`, producing a top-level JSON array. The MCP specification requires `structuredContent` to be a JSON object; strict clients (e.g. pydantic-based SDKs) reject the array with a `dict_type` ValidationError. The C# SDK's `CallToolResult.StructuredContent` is `JsonElement?`, so the in-repo raw JSON-RPC tests never surface the failure.

## Goals / Non-Goals

**Goals:**
- Return both lists as a JSON object so `structuredContent` validates for strict clients.
- Keep the advertised `outputSchema` consistent with the returned shape.
- Leave Core, the REST endpoints, and SQLite behavior untouched.

**Non-Goals:**
- Migrating the MCP test suite onto the real `ModelContextProtocol` client.
- Changing the REST list endpoints or their top-level array shape.

## Decisions

### Wrap both lists in an object with a single `tasks` key

Both tools return an object `{ "tasks": [...] }`. A single shared key keeps the contract uniform and mirrors the tool names (`list_one_shot_tasks`, `list_recurring_tasks`), even though recurring items are templates.

### Reuse `Run<T>` via wrapper records instead of a new helper

Introduce `McpTaskListResponse(IReadOnlyList<McpTaskResponse> Tasks)` and `McpRecurringTemplateListResponse(IReadOnlyList<McpRecurringTemplateResponse> Tasks)`, and have each list method wrap its `.Select(...).ToArray()` in the record. `Run<T>` then serializes the wrapper into both `StructuredContent` and the `Content` text block, so the backward-compatibility "serialized copy in a text block" guidance holds without new plumbing.

Alternative: add a `RunList<T>` helper that wraps an anonymous object. Rejected because `OutputSchemaType` needs a concrete type and it duplicates `Run`'s error handling.

### Change `OutputSchemaType` to the wrapper type

The two list tools currently advertise `typeof(McpTaskResponse[])` / `typeof(McpRecurringTemplateResponse[])`. Change them to the wrapper records so the advertised output schema matches the `{ "tasks": [...] }` object actually returned.

## Risks / Trade-offs

- [Strict clients parse the wrapper differently] -> The `tasks` key is simple and uniform; clients that already handled the array will need to read the nested `tasks` field.
- [The C# client does not validate this] -> The regression guard is an explicit `ValueKind == Object` assertion in the Host tests, not the client SDK.