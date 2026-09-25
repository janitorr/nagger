# Tasks

## 1. Core domain

- [ ] 1.1 Add a `CurrentInstance` property to `RecurringTaskTemplate` that returns the single open (`active` or `paused`) instance or `null`. Verify `dotnet build Nagger.slnx` succeeds.
- [ ] 1.2 Add Core unit tests for `CurrentInstance` covering an active instance, a paused instance, and a template with no open instance. Verify `dotnet test tests/Nagger.Core.Tests` passes.
- [ ] 1.3 Run `dotnet stryker` and add tests for any newly surviving mutants from `CurrentInstance`. Verify the Core mutation score stays at or above 75% (target 80%).

## 2. Host hydration and contract

- [ ] 2.1 Update `SqliteRecurringTaskTemplateStore.GetAllAsync` to hydrate open instances with the same filtered `.Include` used by `GetByIdAsync`. Verify `dotnet build Nagger.slnx` succeeds and existing Host tests still pass.
- [ ] 2.2 Add the nullable `nextDueAt` field to `RecurringTemplateResponse` (REST) and `McpRecurringTemplateResponse` (MCP), mapping from `template.CurrentInstance?.DueAt`. Verify `dotnet build Nagger.slnx` succeeds.
- [ ] 2.3 Add Host integration tests: `GET /tasks/recurring` and MCP `list_recurring_tasks` return `nextDueAt` equal to the open instance's due timestamp for active templates, and `null` for a cancelled template. Verify `dotnet test tests/Nagger.Host.Tests` passes.
- [ ] 2.4 Update the list sections of `USAGE.md` to document `nextDueAt` and its `null`-when-no-open-instance semantics. Verify the documented examples match the implemented responses.

## 3. Integration check

- [ ] 3.1 Run `dotnet test Nagger.slnx` and `dotnet csharpier format .`, then confirm the full suite passes and formatting is clean.
