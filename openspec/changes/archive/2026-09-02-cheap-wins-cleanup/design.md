## Context

Seven independent fixes, each small and self-contained. No spec changes (`skip_specs: true`), no new dependencies, no schema changes. Each maps to a single commit on one branch.

## Goals / Non-Goals

**Goals:**

- Land the cheap wins as one PR with one commit per fix, each independently reviewable.
- Keep every fix behavior-preserving except #57, which only adds a body to an already-404 response.

**Non-Goals:**

- #43 (atomicity), #44 (persistence case), #45 (MCP sanitization), #46 (NU1903), #49 (FK/index), #50 (response record parity), #51 (warnings-as-errors), #53 (timezone caching), #54 (single `now`). These are larger or coupled and out of scope for this batch.
- Adding a validation upper bound on `recurrence.every` (#55 mentions it as a "prefer"); that is a contract change and belongs to its own change.

## Decisions

**#42 — expose `ScenarioNow` as `internal static` on `NaggerFactory`.**
The two `FutureStartDate()` helpers (`ApiTests.cs`, `McpTests.cs`) read `DateTime.UtcNow`. Exposing the existing `private static readonly DateTimeOffset ScenarioNow` lets both derive `ScenarioNow.Date.AddDays(7)`. The fixed clock is `2026-08-03T06:00Z` (Helsinki EEST, +03:00), so the existing hardcoded `+03:00` assertions remain correct. Alternative (deriving expected offset via timezone logic) adds machinery for no benefit at a fixed date.

**#58 — explicit `parsed` bool, branching the past-check on it.**
`TryParseExact` already reports success; discarding it and inferring from `dueAt != default` / `startDate != default` conflates "parse failed" with "parsed to epoch". Capture the bool and guard the past-check with it, preserving error precedence (format error only on failed parse).

**#55 — delegate to `DateOnly.AddMonths`.**
`DateOnly.AddMonths` already clamps to the target month's last day (31 Jan + 1 month → 28/29 Feb), matching the hand-rolled loop. The two existing `CalculateNextDue` tests are the equivalence proof.

**#56 — statement-bodied `switch` in `TryParse`, delete `Set`.**
A conventional `switch` with direct `unit = ...; return true/false` is shorter and matches the sibling status helpers' shape. Case-sensitivity and `default` on failure are preserved.

**#57 — `is TaskNotFoundException or RecurringTaskNotFoundException` → `Results.Problem(statusCode: 404)`.**
`AllowStatusCode404Response = true` is already set, so the handler owns the 404. Using `Results.Problem` (title defaults to "Not Found") keeps the body generic — no id or exception message leaked — and matches the `application/problem+json` shape of the validation and 500 paths. MCP is unaffected (it handles not-found separately).

**#52 — correct `product-brief.md` to match shipped behavior.**
Update the report example to `schemaVersion: "4"` with a `type` field on items (per `morning-task-report` spec and `USAGE.md`), mark listing as shipped, remove reminder-emission recording (deleted in `remove-reminder-policy`), and bump `updated:`.

## Risks / Trade-offs

- [Mutation score] The #55/#56/#58 refactors touch code gated at 75% by Stryker → each commit is verified with `dotnet stryker` before moving on.
- [404 body] Clients reading a bare status will now see a body → additive; existing tests assert status only and keep passing.
- [Test clock] Exposing `ScenarioNow` widens its visibility within the test project only → acceptable; it is already the fixture's source of truth.

## Migration Plan

None — no schema or API changes. Deployment is a normal build/test/format pass.
