# Design

## Context

The Host test suite (`tests/Nagger.Host.Tests`) uses a `WebApplicationFactory` with a per-test temporary SQLite database and a fixed `TimeProvider` (`NaggerFactory.ScenarioNow`). It has two contract files — `ApiTests.cs` (REST) and `McpTests.cs` (MCP) — plus `ConfiguredTimeProviderTests.cs`, `MigrationTests.cs`, and the shared `NaggerFactory`/`FixedTimeProvider` fixtures. `McpTaskTools` delegates to the same Mediator handlers as the REST endpoints, so the two suites re-verify the same domain outcomes through different wire formats.

The SQLite adapters are not thin pass-throughs. `SqliteRecurringTaskTemplateStore.GetByIdAsync`/`GetAllAsync` filter instances to the open view (`active` or `paused`), order by id, and convert domain status/unit to and from wire strings; `UpdateAsync` upserts instances by id (adding the new "next" instance after a complete); `SqliteRecurringTaskInstanceReader` filters `status == "active"`. Stryker mutates only `Nagger.Core` (`stryker-config.json`), so this adapter logic is guarded exclusively by Host integration tests today.

## Goals / Non-Goals

**Goals:**

- Define a single, repeatable test for "does this assertion belong in the Host suite?"
- Remove duplicated domain assertions while preserving coverage of wiring, error mapping, wire contract, and logging.
- Keep the adapter mapping/filtering round-trips in Host tests, since nothing else guards them.

**Non-Goals:**

- No production (`src/`) code, API, dependency, or schema changes.
- No changes to `host-integration-test-organization` or `test-assertion-conventions` — file layout and Shouldly/xUnit stay as-is.
- No renames of existing tests solely to apply the Given/When/Then convention.
- No chasing string-literal Stryker survivors.

## Decisions

### Decision 1: A single filter for domain-vs-contract

The test for every Host assertion is: "If this assertion were deleted, would a Core in-memory test still catch the bug?" If yes, the assertion is domain logic and belongs in Core, not Host.

- Contract-level assertions (status code, field name, shape, content type) fail this test — deleting them would leave the wire contract unguarded.
- Adapter-mapping assertions (a row round-trips with the right status string, an instance list excludes done/cancelled) fail this test — Stryker never mutates the stores.
- Domain-outcome assertions (`completedAt` null-vs-set, report ordering, due-state classification, recurrence math) pass this test — they are Core's job.

**Alternatives considered:** asserting only HTTP status codes and dropping all body checks. Rejected — the body *shape* (field names, null-vs-present) is contract, not domain, and is exactly what JSON serialization/deserialization bugs break.

### Decision 2: "DB works" means the adapter round-trips, not that a row was written

Host persistence tests keep verifying that the SQLite stores map domain objects to rows and back correctly — including open-instance filtering, contract-value conversion, and the upsert-on-complete. A bare "a row exists" check would not catch a projection bug.

### Decision 3: MCP tests shrink to protocol + one representative round-trip

Because `McpTaskTools` calls the same handlers as REST, the MCP suite keeps only what is unique to the MCP boundary: protocol negotiation, tool discovery and input schema, SSE framing, `structuredContent` shape, `isError` mapping, and error sanitization — plus one representative create→lifecycle round-trip to prove tool arguments reach the handlers. The mirrored per-operation domain assertions are removed.

**Alternatives considered:** keeping the full per-tool mirror to be "complete". Rejected — it triples maintenance of the same behavior for no additional fault detection.

### Decision 4: The "did not persist on validation failure" check stays at both HTTP boundaries

Verifying that a failed request writes no rows is a wiring concern (the endpoint does not half-write), so it remains in the REST and MCP validation tests. It is distinct from Core's `store.ShouldBeEmpty()`, which verifies the handler validates before touching the store.

## Risks / Trade-offs

- [Risk] Over-trimming removes the only coverage of a store's mapping/filtering → Mitigation: keep the explicit adapter round-trip category (Decision 2), and during apply confirm each surviving Host test maps to a specific store method.
- [Risk] Deleting a Host domain assertion exposes a Core gap (a rule only exercised through Host) → Mitigation: before removing any Host assertion, confirm a Core test covers the rule; add one where missing (task group 3).
- [Risk] MCP consolidation drops an argument-binding bug → Mitigation: retain one error path per tool family and verify binding through the representative round-trip.
- [Trade-off] The mutation score will not signal Host trimming, since Stryker targets Core only → accepted; Host coverage is judged by the retained contract/adapter tests rather than mutation score.
