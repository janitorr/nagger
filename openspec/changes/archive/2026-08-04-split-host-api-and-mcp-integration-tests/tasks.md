## 1. Separate Contract Test Suites

- [x] 1.1 Extract `NaggerFactory` from `ApiTests.cs` into a dedicated shared fixture file without changing its SQLite or timezone configuration.
- [x] 1.2 Move MCP integration tests and their session, request, and response parsing helpers from `ApiTests.cs` to `McpTests.cs`.
- [x] 1.3 Keep REST endpoint, exception-mapping, and operational logging tests in `ApiTests.cs`, retaining REST-only test doubles there.

## 2. Verify Test Organization

- [x] 2.1 Run `dotnet test tests/Nagger.Host.Tests/Nagger.Host.Tests.csproj` and confirm REST and MCP integration coverage passes.
- [x] 2.2 Run `dotnet test Nagger.slnx` to confirm the complete test suite passes.
