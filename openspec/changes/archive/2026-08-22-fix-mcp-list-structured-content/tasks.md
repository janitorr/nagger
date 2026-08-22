## 1. Host MCP Tools

- [x] 1.1 Add `McpTaskListResponse` and `McpRecurringTemplateListResponse` wrapper records to `McpTaskTools.cs` and verify `dotnet build Nagger.slnx` succeeds
- [x] 1.2 Change `list_one_shot_tasks` to use `OutputSchemaType = typeof(McpTaskListResponse)` and wrap its result in the record, verifying the one-shot list MCP test then asserts an object `tasks` shape
- [x] 1.3 Change `list_recurring_tasks` to use `OutputSchemaType = typeof(McpRecurringTemplateListResponse)` and wrap its result in the record, verifying the recurring list MCP test then asserts an object `tasks` shape

## 2. MCP Integration Tests

- [x] 2.1 Update the one-shot list test to assert `structuredContent` has `ValueKind` `Object` and read its `tasks` array, verifying `dotnet test tests/Nagger.Host.Tests/Nagger.Host.Tests.csproj` passes
- [x] 2.2 Update the empty one-shot list test to assert an object with an empty `tasks` array, verifying the test passes
- [x] 2.3 Update the recurring list test to assert an object and read its `tasks` array, verifying the test passes

## 3. Specification And Verification

- [x] 3.1 Update the `mcp-task-api` delta spec with the corrected structured-content shape and the new recurring listing requirement
- [x] 3.2 Run `dotnet test Nagger.slnx` and verify the full suite passes
- [x] 3.3 Run `openspec validate fix-mcp-list-structured-content --strict` and verify the change validates