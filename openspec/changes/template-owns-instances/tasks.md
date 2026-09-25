# Tasks

## 1. Core domain model

- [x] 1.1 Make `RecurringTaskTemplate` the aggregate root: add an `IReadOnlyList<RecurringTaskInstance> Instances`, add `Complete(DateTimeOffset now, TimeZoneInfo localTimeZone)` that completes the active instance and mints the next from `RecurrenceCalculator`, and update `Pause`/`Resume`/`Cancel` to transition the open instances in the list. Verify `dotnet build Nagger.slnx` compiles once the Core slice in 1.4 lands.
- [x] 1.2 Reshape `Ports.cs`: `IRecurringTaskTemplateStore` becomes the aggregate store (`AddAsync`/`GetByIdAsync`/`UpdateAsync`/`GetAllAsync`), and `IRecurringTaskInstanceStore` is replaced by a read-only `IRecurringTaskInstanceReader` exposing only `GetActiveAsync`. Verify the Core in-memory test fakes compile against the new ports.
- [x] 1.3 Rewrite `CreateRecurringTaskHandler`, `CompleteRecurringTaskHandler`, `Pause`/`Resume`/`Cancel` handlers, and `MorningReportHandler` to load → mutate → save through the aggregate store (single `AddAsync`/`UpdateAsync`), extracting the complete result by role (`done` + `active`); `MorningReportHandler` now depends on `IRecurringTaskInstanceReader`. Verify `dotnet build tests/Nagger.Core.Tests` compiles.
- [x] 1.4 Rewrite `RecurringTaskFeatureTests` and the recurring parts of `TaskFeatureTests` against the aggregate API, covering create, complete (done + next), pause/resume/cancel over open instances, the Helsinki late-evening local completion date, and the single-instant timestamp invariant. Verify `dotnet test tests/Nagger.Core.Tests` passes.

## 2. Host persistence

- [x] 2.1 Add the `Instances` navigation to `RecurringTaskTemplateEntity` and configure `HasMany(x => x.Instances).WithOne().HasForeignKey(x => x.RecurringTaskId)` in `NaggerDbContext`; update `NaggerDbContextModelSnapshot` and confirm no new migration is generated. Verify `dotnet test tests/Nagger.Host.Tests` migration tests pass and no new file appears under `Infrastructure/Migrations/`.
- [x] 2.2 Rewrite `SqliteRecurringTaskTemplateStore` to hydrate open (`active`/`paused`) instances on `GetByIdAsync`, return templates without instances on `GetAllAsync`, and reconcile the template + instance graph in a single `SaveChangesAsync` on `AddAsync`/`UpdateAsync`, returning the persisted aggregate with instance ids assigned. Verify `dotnet build Nagger.slnx` and the recurring integration tests in `ApiTests.cs` pass.
- [x] 2.3 Rename `SqliteRecurringTaskInstanceStore` to `SqliteRecurringTaskInstanceReader`, reducing it to `GetActiveAsync`, and update the `PersistenceServiceCollectionExtensions` registration. Verify `dotnet test tests/Nagger.Host.Tests` passes.

## 3. Remove the transaction behavior

- [x] 3.1 Delete `TransactionBehavior` and remove its `IPipelineBehavior` registration from `MediatorServiceCollectionExtensions`. Verify `dotnet build Nagger.slnx` passes with no reference to `TransactionBehavior`.
- [x] 3.2 Remove the two transaction fault-injection tests (`CreateRecurringTask_GivenInstanceStoreFailsOnCreateSave...`, `CompleteRecurringTask_GivenNextInstanceSchedulingFails...`) and the `FailingRecurringTaskInstanceStore` helper from `ApiTests.cs`. Verify `dotnet test tests/Nagger.Host.Tests` passes.

## 4. Final verification

- [x] 4.1 Run `dotnet build Nagger.slnx` and `dotnet test Nagger.slnx`. Verify the full solution builds and all tests pass.
- [x] 4.2 Run `dotnet stryker`. Verify the mutation score stays at or above 75% (target 80%).
- [x] 4.3 Run `dotnet csharpier format .`. Verify no unformatted files.
