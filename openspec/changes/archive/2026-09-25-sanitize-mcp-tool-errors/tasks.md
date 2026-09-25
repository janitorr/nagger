## 1. Logging facade

- [x] 1.1 Add `McpToolFailed` (Error, event 1008) and `McpToolCancelled` (Warning, event 1009) `[LoggerMessage]` methods to `src/Nagger.Host/AppLog.cs`, each with an `Exception` parameter. Verify `dotnet build Nagger.slnx` succeeds.

## 2. Sanitize unexpected failures in McpTaskTools

- [x] 2.1 Inject `ILogger<McpTaskTools>` into the `McpTaskTools` constructor and convert `Run<T>` to an instance method with a `[CallerMemberName] string toolName = ""` parameter. Verify `dotnet build Nagger.slnx` succeeds.
- [x] 2.2 Add a private `const` for `"An unexpected error occurred."`, and two catch clauses after the existing three: `OperationCanceledException` → `AppLog.McpToolCancelled` + generic error, and `Exception` → `AppLog.McpToolFailed` + generic error. Verify `dotnet build Nagger.slnx` succeeds.

## 3. Integration test

- [x] 3.1 Add `Mcp_GivenThrowingStore_WhenCreateRequested_ThenReturnsSanitizedError` to `tests/Nagger.Host.Tests/McpTests.cs` mirroring the REST sanitization test: configure `NaggerFactory` to swap in `ThrowingStore`, call `create_one_shot_task`, then assert `isError` is true, the content text is `"An unexpected error occurred."`, and the response does not contain `"storage failure"`. Verify `dotnet test tests/Nagger.Host.Tests/Nagger.Host.Tests.csproj` passes with existing MCP tests unchanged.

## 4. Formatting and full verification

- [x] 4.1 Run `dotnet csharpier format .` and verify `dotnet csharpier check .` reports no unformatted files.
- [x] 4.2 Run `dotnet test Nagger.slnx` and verify the full suite passes.
