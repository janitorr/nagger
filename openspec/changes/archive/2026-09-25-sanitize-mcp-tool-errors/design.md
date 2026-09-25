## Context

`McpTaskTools.Run<T>` (`src/Nagger.Host/Mcp/McpTaskTools.cs`) wraps every tool body and currently catches only `ValidationException`, `TaskNotFoundException`, and `RecurringTaskNotFoundException`. Any other exception — a SQLite failure, a mapping error, an `ArgumentOutOfRangeException` from a `ToContractValue` helper — propagates into the MCP SDK, which surfaces the raw message to the client.

The REST surface sanitizes the same class of failure via `ApiExceptionHandler`'s catch-all, which returns a 500 with the title "An unexpected error occurred.".

`DispatchLoggingBehavior` logs exceptions thrown *inside* `mediator.Send` at Error, but not exceptions thrown by the response mapping that runs *after* `mediator.Send` returns — for example `McpTaskResponse.From` calling `TaskStatus.ToContractValue()`. Those mapping failures are currently both leaked and unlogged.

`AppLog` (`src/Nagger.Host/AppLog.cs`) is the source-generated logging facade (`[LoggerMessage]` methods, event IDs 1002–1007).

## Goals / Non-Goals

**Goals:**

- Return a generic tool error (no exception message, type, or stack) for any unexpected failure inside a tool.
- Log the real failure server-side, including post-mediator mapping failures.
- Cover the behavior with a host integration test.

**Non-Goals:**

- Changing how domain errors are returned — validation and not-found messages stay exactly as they are.
- Changing the REST surface or its sanitization.
- Matching REST field-for-field; the MCP surface is independent (see #50).

## Decisions

**1. Add a catch-all in `Run<T>` rather than a global MCP error handler or per-tool catches.**
`Run<T>` is the single choke point every tool already goes through, so one `catch (Exception)` clause sanitizes all twelve tools at once. A global handler would sit in the MCP SDK composition and reach further from the domain; per-tool catches would repeat the same logic twelve times.

**2. Reuse `"An unexpected error occurred."` as the generic message.**
A private `const` in `McpTaskTools`, matching the REST title but chosen because it is a safe, human-readable generic string — not because parity is required. The literal also appears in `ApiExceptionHandler`; that duplication is accepted (two small strings, two independent surfaces) rather than introducing a shared contract location.

**3. Log in the catch-all, not just rely on `DispatchLoggingBehavior`.**
`DispatchLoggingBehavior` only wraps `mediator.Send`, so it misses exceptions thrown by the response mapping after the mediator returns. Injecting `ILogger<McpTaskTools>` and logging in the catch-all closes that gap. This also logs mediator-internal failures a second time (see Risks).

**4. Handle `OperationCanceledException` separately at Warning.**
Cancellation is expected, not unexpected. A dedicated clause logs it at Warning and returns the generic error rather than rethrowing, keeping the tool method's `CallToolResult` return contract intact.

**5. Make `Run<T>` an instance method and use `[CallerMemberName]` for the tool name.**
`Run` must reach the injected logger, so it becomes an instance method. `[CallerMemberName] string toolName = ""` yields the calling method name (`CreateOneShotTask`, …) with zero changes to the twelve call sites, consistent with `DispatchLoggingBehavior` logging PascalCase names.

**6. Add two `AppLog` methods with an `Exception` parameter.**
`McpToolFailed` (Error, event 1008) and `McpToolCancelled` (Warning, event 1009). The `Exception` parameter logs the full type and stack while the message template stays clean.

## Risks / Trade-offs

- [Mediator-internal failures are logged twice] — a store failure is logged by `DispatchLoggingBehavior` and again by the catch-all. → Accepted: the MCP line adds tool-name context the dispatch line lacks.
- [Cancellation returns "An unexpected error occurred."] — technically not unexpected, but the client has already gone. → Mitigated by the distinct Warning log; no transport edge cases from rethrowing.
