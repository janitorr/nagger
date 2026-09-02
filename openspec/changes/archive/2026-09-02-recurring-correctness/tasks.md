## 1. Capture a single instant per recurring handler (#54)

- [x] 1.1 In `CompleteRecurringTaskHandler`, `PauseRecurringTaskHandler`, `ResumeRecurringTaskHandler`, and `CancelRecurringTaskHandler`, capture `timeProvider.GetUtcNow()` once and reuse it for every transition and timestamp; verify `dotnet test tests/Nagger.Core.Tests` passes
- [x] 1.2 Add a Core test with an advancing clock asserting the completed instance's `completedAt` equals its `updatedAt` and the next instance's `createdAt` equals its `updatedAt`; verify `dotnet test tests/Nagger.Core.Tests` passes and `dotnet stryker` stays at or above 75%

## 2. Make recurring writes atomic (#43)

- [x] 2.1 Add `TransactionBehavior<TMessage,TResponse>` in `Composition/Mediator/` constrained `where TMessage : ICommand`, beginning/committing/rolling back a transaction on the scoped `NaggerDbContext`; register it scoped in `MediatorServiceCollectionExtensions.cs`; verify `dotnet build Nagger.slnx`
- [x] 2.2 Add a Host integration test where the instance store throws on its second save during create and assert no template row survives; verify `dotnet test tests/Nagger.Host.Tests`
- [x] 2.3 Add a Host integration test where the instance store throws when scheduling the next instance during complete and assert the completion is rolled back (an open instance remains); verify `dotnet test tests/Nagger.Host.Tests`

## 3. Persist recurrence units via contract values (#44)

- [x] 3.1 Add `RecurrenceUnits.FromContractValue` mirroring the status helpers and switch `SqliteRecurringTaskTemplateStore` to `ToContractValue()` on write and `FromContractValue` on read; verify `dotnet test tests/Nagger.Core.Tests` passes
- [x] 3.2 Add a migration that rewrites existing `"Days"`/`"Weeks"`/`"Months"` rows to lowercase; verify the migration applies cleanly and a round-trip Host test asserts the stored column value and parsed unit for each unit; verify `dotnet test tests/Nagger.Host.Tests`

## 4. Add recurring instance FK and indexes (#49)

- [x] 4.1 Configure the relationship and FK (`Restrict`) and indexes on `RecurringTaskId` and `Status` in `NaggerDbContext`; add a migration; verify the migration applies to a database with existing rows and `dotnet test Nagger.slnx` passes

## 5. Final verification

- [x] 5.1 Run `dotnet build Nagger.slnx` and `dotnet test Nagger.slnx` and confirm the full solution builds and all tests pass
- [x] 5.2 Run `dotnet stryker` and confirm the mutation score stays at or above 75%
- [x] 5.3 Run `dotnet csharpier format .` and confirm no unformatted files
