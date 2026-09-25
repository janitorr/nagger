# Proposal

## Why

The Host integration tests re-assert domain logic that Core in-memory tests already own — lifecycle transition outcomes, morning-report classification and ordering, recurrence math, and validation messages. Every domain-rule change therefore forces edits in up to three layers (Core, REST, MCP), and the REST and MCP suites mirror each other almost one-to-one even though `McpTaskTools` delegates to the same Mediator handlers as the REST endpoints.

## What Changes

- Rewrite the `behavior-focused-host-integration-tests` requirement that currently mandates Host coverage of lifecycle and report behaviors, so that Host integration tests assert only four concerns: persistence/adapter wiring, exception-to-response mapping, the wire contract, and operational logging.
- Trim `tests/Nagger.Host.Tests/ApiTests.cs` and `tests/Nagger.Host.Tests/McpTests.cs` to remove domain re-assertions (report ordering and due-state classification, lifecycle transition semantics, recurrence computation, validation rule messages), keeping one happy path and one error path per endpoint plus wire-shape and persistence checks.
- Preserve adapter mapping and filtering coverage in Host tests (the SQLite stores and instance reader project and filter rows), since Stryker mutates only `Nagger.Core` and cannot guard that logic.
- Backfill Core in-memory tests for any domain rule that was previously exercised only through Host integration tests.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `behavior-focused-host-integration-tests`: narrow the "Focused tests preserve Host contract coverage" requirement so Host tests assert wiring, error mapping, wire contract, and logging, and no longer re-assert domain rules owned by Core tests.

## Impact

- Test files: `tests/Nagger.Host.Tests/ApiTests.cs` and `tests/Nagger.Host.Tests/McpTests.cs` (trimmed); `tests/Nagger.Core.Tests/` (augmented only where a moved rule lacks coverage).
- Spec: `openspec/specs/behavior-focused-host-integration-tests/spec.md` (via this change's delta).
- No production (`src/`) code, API, dependency, or database changes — this is test-only.
- Stryker mutation score is unaffected in aggregate (it mutates `Nagger.Core` against Core tests); Core additions may lift per-file scores for files currently near or below threshold (`ManageRecurringTaskLifecycle.cs`).
