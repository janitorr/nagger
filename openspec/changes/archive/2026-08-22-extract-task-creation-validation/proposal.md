## Why

`CreateRecurringTaskHandler` and `CreateOneShotTaskHandler` each open with a long inline block of `if` statements that parse raw command strings into typed values and accumulate a `ValidationException` error dictionary. The handlers are the enforcement point, but the parsing/validation logic is buried in handler code, making the handlers noisy and the rules hard to test and reuse independently.

## What Changes

- Add a `Parse` instance method to `CreateRecurringTaskCommand` that validates and parses the raw command into typed values, throwing the same `ValidationException` on failure.
- Add a `Parse` instance method to `CreateOneShotTaskCommand` that does the same for one-shot tasks.
- Thin both handlers down to a single call to `command.Parse(...)` before persisting, preserving the exact error keys, messages, aggregation behavior, and the "no persistence on validation failure" contract.
- Add `RecurrenceUnits.TryParse` to the domain, mirroring the existing `ReminderPolicies.TryParse`, to replace the handler's private unit-parsing helper.

No breaking changes and no externally observable behavior change: request/response contracts, error bodies, and state transitions are untouched.

## Capabilities

### New Capabilities

None. This is a pure refactor.

### Modified Capabilities

None. The `recurring-task-creation` and `one-shot-task-creation` specs pin behavior (structured validation errors, no persistence on failure), and this change does not alter that behavior. The change opts out of specs with `skip_specs: true`.

## Impact

- `src/Nagger.Core/Tasks/Domain/RecurringTaskTemplate.cs` — add `RecurrenceUnits.TryParse`.
- `src/Nagger.Core/Tasks/CreateRecurringTask.cs` — add `Parse` on the command; simplify the handler.
- `src/Nagger.Core/Tasks/CreateOneShotTask.cs` — add `Parse` on the command; simplify the handler.
- No API surface, EF schema, migration, or dependency changes. Existing Core and Host tests are the drift guard and must pass unchanged.