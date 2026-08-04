## Why

`ApiTests.cs` combines REST endpoint coverage with MCP protocol coverage even though they expose different public contracts and evolve independently. Separating them makes the host integration test suite easier to navigate and prevents MCP-specific transport helpers from obscuring REST API tests.

## What Changes

- Move MCP initialization, tool invocation, response framing helpers, and MCP integration tests into a dedicated `McpTests.cs` file.
- Retain REST endpoint, exception-mapping, and operational logging tests in `ApiTests.cs`.
- Move the reusable SQLite-backed `NaggerFactory` test fixture to its own file so neither contract test suite depends on the other.
- Preserve all existing integration-test behavior, test project membership, and test execution commands.

## Capabilities

### New Capabilities

- `host-integration-test-organization`: Define contract-based organization and shared-fixture placement for Host integration tests.

### Modified Capabilities

None.

## Impact

- Affected test files: `tests/Nagger.Host.Tests/ApiTests.cs` and new focused test/support files in the same project.
- No production code, public HTTP/MCP contract, persistence schema, or dependency changes.
