## Context

`ApiTests.cs` and `McpTests.cs` use the same isolated SQLite-backed host fixture, but several tests combine unrelated public-contract assertions. The largest MCP test verifies session initialization, discovery metadata, multiple lifecycle operations, and reporting in one workflow. The REST suite has smaller but similar combinations. Core tests demonstrate focused behavior coverage and establish the repository's Given/When/Then naming convention for new or modified tests.

## Goals / Non-Goals

**Goals:**
- Make every Host integration-test failure identify one REST, MCP, or logging behavior.
- Preserve existing public-contract coverage while making missing coverage explicit.
- Retain the shared fixture and protocol helpers without coupling assertions to implementation details.

**Non-Goals:**
- Change REST routes, MCP tool definitions, response schemas, lifecycle semantics, persistence, or logging behavior.
- Replace the existing test project, database fixture, xUnit, or Shouldly.
- Refactor Core tests except where required to keep the solution building.

## Decisions

### Split by externally observable contract behavior

Tests will be divided by the public behavior a caller observes: MCP initialization, discovery, each tool result or error condition, REST endpoint response, report projection, exception mapping, or a logging event. Related fields of one response remain in one test because together they define that response contract.

Alternative considered: retain broad workflow tests as smoke tests alongside focused tests. Rejected because the same transport and adapter composition are already exercised by focused integration tests, and duplicate workflows would retain unclear failures without unique coverage.

### Use parameterization only for behaviorally uniform cases

A theory remains appropriate only if every case has identical arrangement and assertions. Tests requiring different source states, response fields, or branches will be separate facts. This keeps failure names meaningful and removes assertion conditionals.

Alternative considered: parameterize all lifecycle actions. Rejected because pause, complete, resume, and cancel have distinct valid preconditions and timestamp contracts.

### Keep helpers limited to transport and arrangement

`InitializeMcpAsync`, `SendMcpAsync`, request creation, and response parsing remain MCP-local helpers. Small setup helpers may create a task or establish a required lifecycle state, but assertions remain in each test so the test name and body expose the behavior under test.

Alternative considered: introduce assertion helpers for task and report responses. Rejected because they could hide contract fields and make a failing test less diagnosable.

## Risks / Trade-offs

- [More focused tests duplicate setup requests] → Keep only low-level transport and arrangement helpers, and favor explicit setup over abstract workflows.
- [Coverage can be lost while splitting existing tests] → Map every current assertion to a focused test and run both Host and full solution suites.
- [Test count increases] → Accept the small execution cost for isolated failures and maintainable contract coverage.
