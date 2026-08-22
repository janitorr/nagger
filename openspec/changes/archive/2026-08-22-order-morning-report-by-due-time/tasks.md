## 1. Core Ordering

- [x] 1.1 Sort the merged `items` list by `DueAt` ascending in `MorningReportHandler.Handle` and verify the existing Core suite still passes with `dotnet test tests/Nagger.Core.Tests`
- [x] 1.2 Add a Core test asserting chronological ordering (overdue → due-today → upcoming, within-day time order, one-shot and recurring interleaved) and verify it passes with `dotnet test tests/Nagger.Core.Tests`

## 2. Host Integration Coverage

- [x] 2.1 Add a Host test asserting the JSON `items` array is ordered chronologically via `GET /reports/morning` and verify it passes with `dotnet test tests/Nagger.Host.Tests`

## 3. Documentation

- [x] 3.1 Update `USAGE.md` to document report ordering and reorder the example `items` array (overdue → due-today → upcoming, earliest due first) and verify the sample reflects the new order
- [x] 3.2 Sync the delta spec into `openspec/specs/morning-task-report/spec.md` so the ordering requirement is recorded in the main spec

## 4. Verification

- [x] 4.1 Run `dotnet test Nagger.slnx` and verify the full suite passes
- [x] 4.2 Run `dotnet stryker` and confirm the Core mutation score stays at or above the 75% break threshold (80% target), adding tests for any surviving mutants
