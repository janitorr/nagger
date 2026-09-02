## Why

A review pass produced 18 open issues (#41–#58). #41 (the recurring-spec sync) is already resolved and merged via PR #59. This change knocks out the cheap, low-risk remainder first: a set of independent refactors, test-hermeticity fixes, a docs correction, and a consistency fix — none of which change any spec-level requirement.

## What Changes

- Archive the three completed but unarchived changes (`address-analyzer-warnings`, `adopt-mediator-dispatch-logging`, `refresh-docs`) under `openspec/changes/archive/` (all `skip_specs: true`).
- #42: derive Host test recurring start dates from the fixture clock instead of `DateTime.UtcNow`, removing a DST time bomb and a real-clock dependency.
- #58: track `TryParseExact` success in an explicit bool in both create commands instead of inferring it from `value != default`.
- #55: replace the hand-rolled `AddMonthsWithEdgeCaseHandling` with `DateOnly.AddMonths`.
- #56: rewrite `RecurrenceUnits.TryParse` as a statement-bodied switch, deleting the `Set` out-param helper.
- #57: merge the two identical not-found branches in `ApiExceptionHandler` and return a `problem+json` body on 404, consistent with the other error responses.
- #52: correct stale claims in `docs/product-brief.md` (report `schemaVersion`, `type` field, listing status, reminder-emission removal).

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None — this change sets `skip_specs: true`. No requirement changes; #57's 404 body addition is a consistency fix (existing specs only mandate the 404 status), and the rest are pure refactors, tests, and docs.

## Impact

- `src/Nagger.Core`: `Tasks/CreateOneShotTask.cs`, `Tasks/CreateRecurringTask.cs`, `Tasks/Domain/RecurrenceCalculator.cs`, `Tasks/Domain/RecurringTaskTemplate.cs`
- `src/Nagger.Host`: `Api/ExceptionHandling/ApiExceptionHandler.cs`
- `tests/Nagger.Host.Tests`: `NaggerFactory.cs`, `ApiTests.cs`, `McpTests.cs`
- `docs/product-brief.md`
- `openspec/changes/` → `openspec/changes/archive/` (three directories)
- No API contract changes. No new dependencies. No schema changes.
