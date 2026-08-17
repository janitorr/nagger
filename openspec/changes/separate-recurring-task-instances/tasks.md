## 1. Core domain: recurring instance slice

- [ ] 1.1 Add `RecurringTaskInstance` record and `RecurringTaskInstanceStatus` enum (active, paused, done, cancelled) with contract-value mapping in `src/Nagger.Core/Tasks/`
- [ ] 1.2 Add `IRecurringTaskInstanceStore` port (`AddAsync`, `GetByIdAsync`, `UpdateAsync`, `GetActiveAsync`, `GetByTemplateIdAsync`) to `Ports.cs`
- [ ] 1.3 Remove `GetByRecurringTaskIdAsync` from `ITaskStore` in `Ports.cs`
- [ ] 1.4 Remove the `RecurringTaskId` field from `TaskItem` in `TaskItem.cs`

## 2. Core handlers

- [ ] 2.1 Update `CreateRecurringTask` to create the first instance through `IRecurringTaskInstanceStore` using the new `RecurringTaskInstance` record
- [ ] 2.2 Rework `CompleteRecurringTaskHandler` to resolve `{id}` as the template id: load template, find its current active instance, complete it, and create the next instance; return a structured validation error when no active instance exists
- [ ] 2.3 Update pause/resume/cancel recurring handlers to read and write instances through `IRecurringTaskInstanceStore`
- [ ] 2.4 Strip recurrence logic from `CompleteOneShotTaskHandler` so it depends only on `ITaskStore` and `IClock`
- [ ] 2.5 Update `MorningReport` to add a `type` field to `MorningReportItem`, aggregate active one-shot tasks and active recurring instances, emit the template id for recurring items, and set `schemaVersion` to `"3"`

## 3. Infrastructure

- [ ] 3.1 Add `RecurringTaskInstanceEntity` and its DbSet/mapping to `NaggerDbContext`, and remove `RecurringTaskId` from `TaskEntity`
- [ ] 3.2 Add an EF migration that creates `recurring_task_instances` and drops the `RecurringTaskId` column from `one_shot_tasks`
- [ ] 3.3 Add `SqliteRecurringTaskInstanceStore` implementing the new port
- [ ] 3.4 Update `SqliteTaskStore` to remove `RecurringTaskId` mapping and `GetByRecurringTaskIdAsync`

## 4. API and MCP

- [ ] 4.1 Update `RecurringTaskEndpoints` so complete returns the completed recurring instance representation (with `type` `"recurring"`)
- [ ] 4.2 Update MCP `complete_recurring_task` to take the template id (from `list_recurring_tasks`) and return the completed recurring instance
- [ ] 4.3 Update MCP tool descriptions so each recurring `id` names its source and no description references reading instance ids from `list_one_shot_tasks` or the morning report

## 5. Tests

- [ ] 5.1 Update Core recurring and one-shot tests for the new ports and handlers; cover complete-by-template, no-active-instance rejection, missing-template not-found, and one-shot complete no longer spawning an instance
- [ ] 5.2 Update Host integration tests for the new endpoints, report `type` field, and `schemaVersion` `"3"`
- [ ] 5.3 Update MCP integration tests for `complete_recurring_task` template-id semantics and report `type`/`schemaVersion`

## 6. Verification

- [ ] 6.1 Run `dotnet build Nagger.slnx` and `dotnet test Nagger.slnx`
- [ ] 6.2 Run Core mutation testing (`dotnet stryker`) and add tests for any surviving mutants
