## Why

Unexpected exceptions raised inside an MCP tool (a SQLite failure, a contract-mapping error, an `ArgumentOutOfRangeException` from a `ToContractValue` helper) fall through `McpTaskTools.Run<T>` into the MCP SDK, which surfaces the raw exception message to the client. The REST surface already sanitizes these to a generic 500, but MCP has no equivalent, so internal detail — exception messages and, in the store-failure case, content derived from task titles — can leak to LLM clients.

## What Changes

- `McpTaskTools.Run<T>` catches unexpected exceptions and returns a generic tool error containing no exception message, type name, or stack detail.
- Cancellation (`OperationCanceledException`) is handled separately: logged at Warning and returned as a generic error rather than treated as an unexpected failure.
- The real failure is logged server-side: unexpected failures at Error, cancellation at Warning.
- A host integration test mirrors the existing REST sanitization test, asserting the leak marker is absent from the tool result.
- The `mcp-task-api` spec gains a requirement that unexpected tool failures are sanitized.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `mcp-task-api`: add a requirement that an unexpected exception inside an MCP tool produces a generic tool error with no internal detail, and that the failure is logged server-side.

## Impact

- `src/Nagger.Host/Mcp/McpTaskTools.cs` — constructor, `Run<T>`, and error handling.
- `src/Nagger.Host/AppLog.cs` — two new source-generated log methods.
- `tests/Nagger.Host.Tests/McpTests.cs` — a new sanitization test.
- No changes to Core, the REST API, or persistence. No breaking changes.
