## 1. Move domain model into Domain/

- [x] 1.1 `git mv` the seven domain files (`TaskItem.cs`, `RecurringTaskTemplate.cs`, `RecurringTaskInstance.cs`, `ReminderPolicy.cs`, `RecurrenceCalculator.cs`, `DateOnlyExtensions.cs`, `Validation.cs`) into `src/Nagger.Core/Tasks/Domain/`; verify the folder contains exactly those 7 files and `Tasks/` root keeps the 7 slices + `Ports.cs`
- [x] 1.2 Change each moved file's `namespace Nagger.Core.Tasks` to `namespace Nagger.Core.Tasks.Domain`; verify no moved file still declares the root namespace

## 2. Fix consumers

- [x] 2.1 Add `using Nagger.Core.Tasks.Domain;` to the 8 remaining `Tasks/` files (`Ports.cs` and the 7 slices) that reference moved types; verify each file compiles
- [x] 2.2 Add `using Nagger.Core.Tasks.Domain;` to the 8 Host files (`Api/TaskEndpoints`, `Api/RecurringTaskEndpoints`, `Api/ExceptionHandling/ApiExceptionHandler`, `Composition/Mediator/MediatorServiceCollectionExtensions`, `Infrastructure/SqliteTaskStore`, `Infrastructure/SqliteRecurringTaskTemplateStore`, `Infrastructure/SqliteRecurringTaskInstanceStore`, `Mcp/McpTaskTools`); verify each references a moved type before adding the using
- [x] 2.3 Add `using Nagger.Core.Tasks.Domain;` to the 3 test files (`RecurringTaskFeatureTests`, `TaskFeatureTests`, `ApiTests`); verify each references a moved type before adding the using

## 3. Verify

- [x] 3.1 Run `dotnet build Nagger.slnx` and verify the build succeeds with no warnings or errors
- [x] 3.2 Run `dotnet test Nagger.slnx` and verify all tests pass
