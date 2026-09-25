# Tasks

## 1. Restructure REST integration tests

- [x] 1.1 Trim morning-report tests in `ApiTests.cs` to assert status code, `schemaVersion`, field names/shapes, and persisted state, removing due-state ordering and classification assertions; verify `dotnet test tests/Nagger.Host.Tests` passes and `MorningReport_GivenMixedDueStates` no longer asserts the chronological item ID order.
- [x] 1.2 Trim one-shot lifecycle tests to assert HTTP status, response field shape, and persisted row status, removing `completedAt`/`cancelledAt` null-vs-set domain semantics; verify the transition rules remain covered by `tests/Nagger.Core.Tests/TaskFeatureTests.cs`.
- [x] 1.3 Trim recurring lifecycle tests the same way; verify `tests/Nagger.Core.Tests/RecurringTaskFeatureTests.cs` still covers template and instance transition rules.
- [x] 1.4 Preserve adapter round-trip assertions for the SQLite stores — open-instance filtering, `nextDueAt` projection, instance upsert on complete; verify recurring create/complete/list tests still assert database row state.

## 2. Restructure MCP integration tests

- [x] 2.1 Keep protocol negotiation, tool discovery/schema, and SSE framing tests, removing per-tool domain re-assertions (report classification, transition semantics); verify `dotnet test tests/Nagger.Host.Tests` passes.
- [x] 2.2 Keep one representative create-to-lifecycle round-trip plus the error-to-`isError` and sanitization tests, removing the mirrored per-operation domain checks; verify MCP error mapping and sanitization remain covered.

## 3. Backfill Core coverage for moved rules

- [x] 3.1 For each domain assertion removed from Host, confirm a Core in-memory test already covers it and add one where missing; verify `dotnet test tests/Nagger.Core.Tests` passes.

## 4. Mutation testing and formatting

- [x] 4.1 Run `dotnet stryker` and confirm the mutation score remains at or above the 75% break threshold (target 80%), adding Core tests for any surviving mutants the move exposes.
- [x] 4.2 Run `dotnet csharpier format .` and confirm `dotnet csharpier check .` passes.
