## Context

The Host integration tests execute REST endpoints and MCP tools through the same `WebApplicationFactory` and temporary SQLite database. Their current single source file combines REST-specific response assertions with MCP session negotiation, JSON-RPC requests, and server-sent event response parsing. The reusable factory is also declared in that mixed file.

## Goals / Non-Goals

**Goals:**
- Give REST and MCP integration coverage separate, contract-named test files.
- Keep shared host setup available to both suites without coupling either suite to the other.
- Preserve test behavior, isolation, and execution within `Nagger.Host.Tests`.

**Non-Goals:**
- Change REST routes, MCP tool definitions, JSON contracts, or error handling.
- Create a separate test project or alter test infrastructure dependencies.
- Reorganize Core tests or make unrelated test naming changes.

## Decisions

### Keep both suites in the existing Host test project

REST and MCP tests both exercise the same in-process host, dependency injection composition root, and SQLite persistence adapter. A separate project would duplicate dependencies and fixture setup without adding a meaningful execution boundary.

Alternative considered: create an MCP-specific test project. Rejected because MCP is another Host adapter and shares the same required host-level integration fixture.

### Separate tests by external contract

`ApiTests.cs` will contain REST endpoint, exception handling, and logging tests. `McpTests.cs` will contain MCP tool workflow and error tests, together with the MCP request/session/response helpers they exclusively use.

Alternative considered: retain one file and use nested regions or test classes. Rejected because file-level separation makes each contract independently discoverable and keeps protocol-specific support close to its callers.

### Extract the host fixture into focused test support

`NaggerFactory` will move to `NaggerFactory.cs`. It remains public for `WebApplicationFactory` usage and continues to configure a unique temporary SQLite database and configured timezone per fixture instance. REST-only doubles remain beside their REST tests.

Alternative considered: leave `NaggerFactory` in `ApiTests.cs`. Rejected because `McpTests` would then depend on an API-named source file for shared infrastructure.

## Risks / Trade-offs

- [Moving tests can accidentally omit or duplicate coverage] → Move complete test methods with their required helpers, then run the Host test project and the full solution test suite.
- [Fixture disposal behavior could change during extraction] → Preserve its implementation and validate that test-created SQLite files are still cleaned up.
- [The term "API" can technically include MCP over HTTP] → Use the repository’s REST endpoint terminology in `ApiTests` and reserve `McpTests` for the distinct JSON-RPC/MCP contract.
