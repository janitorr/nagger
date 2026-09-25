# Proposal

## Why

Recurring writes are atomic only because a Host-level Mediator `TransactionBehavior` wraps command dispatch in a database transaction, while the domain still models a template and its instances as two independent aggregates joined by a loose `RecurringTaskId`. Atomicity is an invisible infrastructure side effect, not a property of the data model, so any future write path that bypasses dispatch silently loses it.

## What Changes

- Make `RecurringTaskTemplate` the aggregate root: the record carries an `IReadOnlyList<RecurringTaskInstance>` (its open instances — `active`/`paused`).
- Create/complete/pause/resume/cancel mutate the loaded aggregate in memory and persist the whole graph in a single `SaveChanges`, atomic by construction.
- Remove the Host `TransactionBehavior`; atomicity is no longer a dispatch-layer concern.
- Shrink `IRecurringTaskInstanceStore` to a read-only `IRecurringTaskInstanceReader` exposing only `GetActiveAsync`, used by the morning report (CQRS read).
- Keep `done`/`cancelled` instances persisted as inert history, but never hydrate them into the aggregate.
- Move the next-due-date computation into the aggregate: `Complete(DateTimeOffset now, TimeZoneInfo localTimeZone)`.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None — this change sets `skip_specs: true`. No requirement changes; every observable contract (HTTP, MCP tools, morning report) is unchanged. All edits are internal model, persistence, and dispatch concerns.

## Impact

- `src/Nagger.Core/Tasks/Domain/RecurringTaskTemplate.cs`, `RecurringTaskInstance.cs` — aggregate root and open-instance hydration.
- `src/Nagger.Core/Tasks/Ports.cs` — aggregate store surface plus the read-only `IRecurringTaskInstanceReader`.
- `src/Nagger.Core/Tasks/CreateRecurringTask.cs`, `ManageRecurringTaskLifecycle.cs`, `ListRecurringTasks.cs`, `MorningReport.cs` — handlers thin out to load → mutate → save.
- `src/Nagger.Host/Infrastructure/SqliteRecurringTaskTemplateStore.cs`, `SqliteRecurringTaskInstanceStore.cs` (→ reader), `NaggerDbContext.cs` + model snapshot.
- `src/Nagger.Host/Composition/Mediator/TransactionBehavior.cs` (deleted), `MediatorServiceCollectionExtensions.cs`, `Composition/Persistence/PersistenceServiceCollectionExtensions.cs`.
- `tests/Nagger.Core.Tests`, `tests/Nagger.Host.Tests` — rewrite recurring feature tests against the aggregate; remove the transaction fault-injection tests.
- No API/HTTP/MCP contract changes, no new dependencies, no schema migrations.
