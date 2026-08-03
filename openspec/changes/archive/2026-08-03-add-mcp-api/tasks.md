## 1. MCP Host Setup

- [x] 1.1 Add the compatible .NET MCP ASP.NET Core server package to `Nagger.Host`.
- [x] 1.2 Register the MCP server and streamable-HTTP transport in host composition, and map its endpoint without changing existing REST mappings.

## 2. MCP Task Adapter

- [x] 2.1 Add dedicated MCP tool definitions and structured task/report response records that map existing Core results without reusing HTTP endpoint handlers.
- [x] 2.2 Implement `create_one_shot_task` by dispatching `CreateOneShotTaskCommand` and returning the persisted task representation.
- [x] 2.3 Implement complete, pause, resume, and cancel tools by dispatching their corresponding Core lifecycle commands and returning updated tasks.
- [x] 2.4 Implement `get_morning_report` by dispatching `MorningReportQuery` and returning its structured report representation.
- [x] 2.5 Map expected Core validation and missing-task failures to non-successful MCP tool results while allowing unexpected failures to use normal host diagnostics.

## 3. Verification And Documentation

- [x] 3.1 Add host integration tests for MCP initialization, tool discovery, successful task creation and lifecycle operations, and morning-report reads against temporary SQLite storage.
- [x] 3.2 Add host integration tests that verify invalid MCP inputs, invalid lifecycle transitions, and unknown task ids return tool errors without unintended persistence changes.
- [x] 3.3 Document the streamable-HTTP MCP endpoint, available tools, and local-only scope in `USAGE.md`.
- [x] 3.4 Run `dotnet test Nagger.slnx` and `openspec validate add-mcp-api --strict`.
