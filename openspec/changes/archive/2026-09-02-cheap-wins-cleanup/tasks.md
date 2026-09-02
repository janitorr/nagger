## 1. Archive completed changes

- [x] 1.1 Move `address-analyzer-warnings`, `adopt-mediator-dispatch-logging`, and `refresh-docs` into `openspec/changes/archive/2026-09-02-<name>/` and verify `openspec list --json` reports no active changes

## 2. Fix the DST time bomb (#42)

- [x] 2.1 Expose `NaggerFactory.ScenarioNow` as `internal static` and change both `FutureStartDate()` helpers (`ApiTests.cs`, `McpTests.cs`) to derive from it; verify `grep -r "DateTime.UtcNow\|DateTimeOffset.UtcNow" tests/` returns no matches and `dotnet test tests/Nagger.Host.Tests` passes

## 3. Track parse success explicitly (#58)

- [x] 3.1 Capture the `TryParseExact` result in a named bool in `CreateOneShotTaskCommand.Parse` and `CreateRecurringTaskCommand.Parse`, branching the past-check on it; verify `dotnet test tests/Nagger.Core.Tests` passes and `dotnet stryker` shows no new surviving mutants

## 4. Use DateOnly.AddMonths (#55)

- [x] 4.1 Delete `AddMonthsWithEdgeCaseHandling` and route the `Months` arm through `completionDate.AddMonths(rule.Every)`; verify the two `CalculateNextDue` tests pass unchanged and `dotnet stryker` stays at or above 75%

## 5. Simplify RecurrenceUnits.TryParse (#56)

- [x] 5.1 Rewrite `RecurrenceUnits.TryParse` as a statement-bodied switch, delete the `Set` helper, and preserve case-sensitivity and `default`-on-failure; verify the `RecurrenceUnits_*` tests pass unchanged and `dotnet stryker` stays at or above 75%

## 6. Return problem details on 404 (#57)

- [x] 6.1 Merge the two not-found branches into `is TaskNotFoundException or RecurringTaskNotFoundException` returning `Results.Problem(statusCode: 404)`; verify existing not-found tests pass and add a test asserting the 404 content type and body shape

## 7. Correct the product brief (#52)

- [x] 7.1 Update `docs/product-brief.md`: report example `schemaVersion` "4" with `type` on items, listing shipped, reminder-emission recording removed, and `updated:` frontmatter bumped; verify the brief no longer contradicts `USAGE.md` or `openspec/specs/`

## 8. Final verification

- [x] 8.1 Run `dotnet build Nagger.slnx` and `dotnet test Nagger.slnx` and confirm the full solution builds and all tests pass
- [x] 8.2 Run `dotnet csharpier format .` and confirm no unformatted files
