## 1. Domain Helper

- [x] 1.1 Add `RecurrenceUnits.TryParse(string?, out RecurrenceUnit)` to `src/Nagger.Core/Tasks/Domain/RecurringTaskTemplate.cs` mirroring `ReminderPolicies.TryParse` and verify `dotnet build Nagger.slnx` succeeds
- [x] 1.2 Add a Core test for `RecurrenceUnits.TryParse` covering the three valid values and an invalid value, and verify it passes in `tests/Nagger.Core.Tests`

## 2. Recurring Task Command

- [x] 2.1 Add `CreateRecurringTaskCommand.Parse(DateOnly today)` returning `(string Title, DateOnly StartDate, RecurrenceRule Recurrence, ReminderPolicy ReminderPolicy)` and throwing `ValidationException` on failure, preserving the existing error keys, messages, aggregation, and the `startDate != default` guard before the past-date check
- [x] 2.2 Replace the inline validation block in `CreateRecurringTaskHandler` with `var (title, startDate, recurrence, reminderPolicy) = command.Parse(Today());` and remove the private `TryParseUnit`/`Set` helpers, using `RecurrenceUnits.TryParse` from task 1.1
- [x] 2.3 Verify all recurring creation tests in `RecurringTaskFeatureTests` pass unchanged

## 3. One-Shot Task Command

- [x] 3.1 Add `CreateOneShotTaskCommand.Parse()` returning `(string Title, DateTimeOffset DueAt, ReminderPolicy ReminderPolicy)` and throwing `ValidationException` on failure, preserving the existing error keys, messages, and aggregation
- [x] 3.2 Replace the inline validation block in `CreateOneShotTaskHandler` with `var (title, dueAt, reminderPolicy) = command.Parse();`
- [x] 3.3 Verify all one-shot creation tests in `TaskFeatureTests` pass unchanged

## 4. Verification

- [x] 4.1 Run `dotnet build Nagger.slnx` and verify it succeeds
- [x] 4.2 Run `dotnet test Nagger.slnx` and verify all Core and Host tests pass, including the Host tests asserting the validation error body (`ApiTests.cs`)
- [x] 4.3 Run `dotnet stryker` and verify the mutation score stays at or above 75%; add direct `Parse` tests for any newly surviving mutants and re-run to confirm