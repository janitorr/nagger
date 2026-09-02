## Why

Recurring task writes have four correctness defects: each operation reads the clock multiple times so timestamps can skew; create/complete are not atomic so a mid-write failure orphans a template or silently ends recurrence; recurrence units persist in a different case convention than every other enum; and instances lack the referential integrity and indexes the hot paths assume. None changes the API contract, but each can produce wrong or corrupt state.

## What Changes

- #54: capture the current instant once per recurring lifecycle handler, so a next instance's `createdAt`/`updatedAt`/`completedAt` never skew.
- #43: wrap recurring command dispatch in a database transaction (Host-level Mediator behavior), so create/complete/pause/resume/cancel are all-or-nothing.
- #44: persist recurrence units through a contract-value pair (`"days"`/`"weeks"`/`"months"`), mirroring the status enums, with a migration rewriting existing rows.
- #49: add a foreign key from recurring instances to their template and index the `RecurringTaskId`/`Status` columns the queries filter on.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None — this change sets `skip_specs: true`. No requirement changes; all fixes are internal correctness and persistence behavior invisible at the contract level.

## Impact

- `src/Nagger.Core/Tasks/ManageRecurringTaskLifecycle.cs` (#54)
- `src/Nagger.Core/Tasks/Domain/RecurringTaskTemplate.cs` (#44, new `FromContractValue`)
- `src/Nagger.Host/Composition/Mediator/` (new `TransactionBehavior`) + `MediatorServiceCollectionExtensions.cs` (#43)
- `src/Nagger.Host/Infrastructure/SqliteRecurringTaskTemplateStore.cs` (#44)
- `src/Nagger.Host/Infrastructure/NaggerDbContext.cs` + two migrations (#44, #49)
- `tests/Nagger.Core.Tests`, `tests/Nagger.Host.Tests`
- No API contract changes, no new dependencies, no spec changes.
