## Why

`src/Nagger.Core/Tasks/` mixes three layers — the shared domain model, the persistence/time ports, and the Mediator vertical slices — in one flat folder, so it is hard to tell at a glance what is a domain concept versus an application entry point. Extracting the shared domain model into a `Domain/` subfolder with a matching sub-namespace makes the layering explicit, and builds on the `.editorconfig` IDE0130 convention added just before this change.

## What Changes

- Move the seven shared domain-model files into `src/Nagger.Core/Tasks/Domain/`:
  `TaskItem.cs`, `RecurringTaskTemplate.cs`, `RecurringTaskInstance.cs`, `ReminderPolicy.cs`, `RecurrenceCalculator.cs`, `DateOnlyExtensions.cs`, `Validation.cs`.
- Change their namespace from `Nagger.Core.Tasks` to `Nagger.Core.Tasks.Domain`.
- Add `using Nagger.Core.Tasks.Domain;` to the files that consume the moved types (remaining `Tasks/` files, Host adapters/endpoints, and Core/Host tests).
- No changes to ports, slices, EF mappings, migrations, or externally observable behavior.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This is a pure internal reorganization with no behavior change, so it opts out of specs via `skip_specs: true`.

## Impact

- Core: 7 files move into `Tasks/Domain/`; 8 remaining `Tasks/` files (slices + `Ports.cs`) gain a using.
- Host: `Api/TaskEndpoints`, `Api/RecurringTaskEndpoints`, `Api/ExceptionHandling/ApiExceptionHandler`, `Composition/Mediator/MediatorServiceCollectionExtensions`, `Infrastructure/SqliteTaskStore`, `Infrastructure/SqliteRecurringTaskTemplateStore`, `Infrastructure/SqliteRecurringTaskInstanceStore`, `Mcp/McpTaskTools` gain a using.
- Tests: `RecurringTaskFeatureTests`, `TaskFeatureTests`, `ApiTests` gain a using.
- No API, database, or dependency changes. EF migrations/snapshot store enums as `string` and do not reference the moved types.
