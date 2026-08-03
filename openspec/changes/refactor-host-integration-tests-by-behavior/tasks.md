## 1. Refactor MCP Contract Coverage

- [x] 1.1 Split MCP initialization, tool discovery, and tool-metadata assertions into focused protocol tests in `McpTests.cs`.
- [x] 1.2 Split successful MCP create, lifecycle, cancellation, and morning-report assertions into focused tool-behavior tests in `McpTests.cs`.
- [x] 1.3 Split MCP invalid-creation, missing-task, and invalid-transition assertions into focused error-behavior tests that verify unchanged state where required.

## 2. Refactor REST And Logging Coverage

- [x] 2.1 Split REST task creation, report projection, validation, upcoming-task, and repeatability assertions into focused endpoint-behavior tests in `ApiTests.cs`.
- [x] 2.2 Refactor REST lifecycle and operational-logging tests so parameterized cases have uniform assertions and distinct behavior contracts have separate tests.
- [x] 2.3 Rename new or modified Host tests to the `Subject_GivenCondition_WhenAction_ThenOutcome` convention.

## 3. Verify Focused Coverage

- [x] 3.1 Run `dotnet test tests/Nagger.Host.Tests/Nagger.Host.Tests.csproj` and confirm all focused REST and MCP integration tests pass.
- [x] 3.2 Run `dotnet test Nagger.slnx` and confirm the complete test suite passes.
