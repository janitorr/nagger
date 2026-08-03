## Why

Host integration tests currently combine unrelated REST and MCP behaviors in single test methods. A failure therefore does not identify the broken contract, and extending one behavior requires editing an unrelated test workflow.

## What Changes

- Refactor REST and MCP Host integration tests so each test verifies one externally observable behavior.
- Preserve existing REST routes, MCP tools, response contracts, persistence behavior, and test infrastructure.
- Keep shared host setup and MCP transport helpers focused on arrangement rather than obscuring the behavior being tested.

## Capabilities

### New Capabilities
- `behavior-focused-host-integration-tests`: Define focused behavioral coverage expectations for Host REST and MCP integration tests.

### Modified Capabilities

None.

## Impact

- Affected test files: `tests/Nagger.Host.Tests/ApiTests.cs` and `tests/Nagger.Host.Tests/McpTests.cs`.
- No production code, public API/MCP contracts, persistence schema, or dependencies change.
