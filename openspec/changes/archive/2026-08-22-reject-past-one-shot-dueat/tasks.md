## 1. Core Validation

- [x] 1.1 Change `CreateOneShotTaskCommand.Parse()` to accept the current instant (`Parse(DateTimeOffset now)`) and reject a past `dueAt` with `if (dueAt != default && dueAt < now) errors["dueAt"] = ["Due timestamp cannot be in the past."]`; update `CreateOneShotTaskHandler.Handle` to pass `clock.UtcNow`. Verify `dotnet build Nagger.slnx` succeeds.

## 2. Core Tests

- [x] 2.1 Add `CreateTask_GivenPastDueAt_WhenCreateRequested_ThenRejectsTask` in `tests/Nagger.Core.Tests/TaskFeatureTests.cs`, asserting a past `dueAt` throws `ValidationException` with the `dueAt` key and nothing is persisted. Verify `dotnet test tests/Nagger.Core.Tests`.
- [x] 2.2 Add `CreateTask_GivenDueAtEqualToNow_WhenCreateRequested_ThenCreatesTask`, pinning that `dueAt == now` is allowed (strict `<` comparison). Verify `dotnet test tests/Nagger.Core.Tests`.

## 3. Host Integration Tests

- [x] 3.1 Add `CreateOneShotTask_GivenPastDueAt_WhenCreateRequested_ThenReturnsValidationErrorWithoutPersistingTask` in `tests/Nagger.Host.Tests/ApiTests.cs`, asserting `400 Bad Request`, a `dueAt` error, and no persisted task. Verify `dotnet test tests/Nagger.Host.Tests`.

## 4. Documentation

- [x] 4.1 Update the `dueAt` row in `USAGE.md` to state the timestamp must not be in the past, matching the recurring `startDate` wording. Verify the table reads consistently with the recurring rule.

## 5. Verification

- [x] 5.1 Run `dotnet stryker` against Core and confirm the mutation score stays at or above 75% (target 80%); add tests for any mutants the new guard leaves surviving. Verify the command exits successfully.

## 6. Clock Abstraction Refactor

- [x] 6.1 Replace `IClock` with `System.TimeProvider` across Core: remove the `IClock` port, inject `TimeProvider` into `CreateOneShotTaskHandler`, `CreateRecurringTaskHandler`, the four one-shot lifecycle handlers, the four recurring lifecycle handlers, and `MorningReportHandler`, mapping `UtcNow` → `GetUtcNow()` and `TimeZone` → `LocalTimeZone`. Verify `dotnet build Nagger.slnx` succeeds.
- [x] 6.2 Replace `ConfiguredClock` with a `TimeProvider` subclass (`ConfiguredTimeProvider`) in Host that overrides `LocalTimeZone` from `Nagger:TimeZone`; register it in `PersistenceServiceCollectionExtensions` (replacing `AddSingleton<IClock, ConfiguredClock>`) and drop the now-unused `IClock` wiring. Verify `dotnet build Nagger.slnx` succeeds.
- [x] 6.3 Replace the Core test `TestClock` subclasses and the Host `FixedClock` with fake `TimeProvider` subclasses. Verify `dotnet test` on both test projects passes.
