## 1. Core domain and create commands

- [x] 1.1 Delete `src/Nagger.Core/Tasks/Domain/ReminderPolicy.cs` and remove the `ReminderPolicy` property from `TaskItem`, `RecurringTaskTemplate`, and `RecurringTaskInstance`; verify `dotnet build Nagger.slnx` fails only on now-orphaned references.
- [x] 1.2 Remove the `reminderPolicy` parameter and its validation from `CreateOneShotTaskCommand` and its handler; verify `dotnet build Nagger.slnx` succeeds.
- [x] 1.3 Remove the `reminderPolicy` parameter and its validation from `CreateRecurringTaskCommand` and its handler; verify `dotnet build Nagger.slnx` succeeds.

## 2. Host REST endpoints

- [x] 2.1 Remove `reminderPolicy` from the one-shot create request/response contracts in `src/Nagger.Host/Api/TaskEndpoints.cs`; verify `dotnet build Nagger.slnx` succeeds.
- [x] 2.2 Remove `reminderPolicy` from the recurring create request and the template/instance representations in `src/Nagger.Host/Api/RecurringTaskEndpoints.cs`; verify `dotnet build Nagger.slnx` succeeds.

## 3. Host MCP tools

- [x] 3.1 Remove the `reminderPolicy` parameter from `create_one_shot_task` and `create_recurring_task` and the `ReminderPolicy` field from all `McpTaskTools` response records; verify `dotnet build Nagger.slnx` succeeds.

## 4. Persistence

- [x] 4.1 Remove `ReminderPolicy` and `LastReminderAt` from the EF entities and mappings in `NaggerDbContext`, `SqliteTaskStore`, `SqliteRecurringTaskTemplateStore`, and `SqliteRecurringTaskInstanceStore`; verify `dotnet build Nagger.slnx` succeeds.
- [x] 4.2 Generate a new EF migration that drops the `ReminderPolicy` columns from tasks/templates/instances and `LastReminderAt` from tasks; verify the Host starts and applies the migration against a pre-change `nagger.db` without error.

## 5. Report

- [x] 5.1 Bump the report `schemaVersion` from `"3"` to `"4"` and remove `reminderPolicy` from report items in `MorningReport.cs` and `ReportEndpoints.cs`; verify `dotnet build Nagger.slnx` succeeds.

## 6. Tests

- [x] 6.1 Update Core and Host test fixtures/assertions to stop sending and asserting `reminderPolicy`, and remove the "unsupported reminder policy" scenarios; verify `dotnet test Nagger.slnx` passes.
- [x] 6.2 Run `dotnet stryker` and confirm the mutation score stays at or above 75%; add tests for any newly surviving mutants.

## 7. Docs

- [x] 7.1 Update `USAGE.md`, `README.md`, `docs/product-brief.md`, and `docs/product-design.md` to remove `reminderPolicy` and the "reminder delivery" plans; verify `rg "reminderPolicy|weekly-until-done|reminder delivery|reminders/emitted"` returns no matches in the repo.
