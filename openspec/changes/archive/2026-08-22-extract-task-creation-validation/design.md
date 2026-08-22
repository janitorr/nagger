## Context

Both creation handlers (`CreateRecurringTask.cs`, `CreateOneShotTask.cs`) start with an inline block that parses raw command strings into typed values and accumulates a `ValidationException` error dictionary. The handler is the enforcement point, and the 400 contract (field keys + messages, multi-field aggregation) is pinned by Core and Host tests. See proposal.md - Why for motivation. The domain already exposes `ReminderPolicies.TryParse` and `RecurrenceUnits.ToContractValue`, but unit parsing lives as a private helper in the recurring handler.

## Goals / Non-Goals

**Goals:**

- Move parse + validate logic out of the handlers into a cohesive `Parse` method on each command, keeping the handler as the enforcement point.
- Preserve the exact `ValidationException` error dictionary (keys, messages, aggregation) and the "throw before any persistence" behavior.
- Make the extracted logic independently testable and reusable by both transports (HTTP and MCP) without changing either.

**Non-Goals:**

- No typing of the command fields (`string?` stays) — the command remains the transport DTO bound from JSON.
- No Mediator pipeline behavior, no separate validator types, no new record types.
- No change to `CreateOneShotTaskCommand`'s lack of a past-date rule.

## Decisions

### Decision: `Parse` is an instance method on the command

`CreateRecurringTaskCommand.Parse(DateOnly today)` and `CreateOneShotTaskCommand.Parse()` return a named tuple of typed values and throw `ValidationException` on failure. The command record sits in `Nagger.Core.Tasks` (application layer) and already references `Nagger.Core.Tasks.Domain`, so it can parse using domain helpers without inverting layering.

- Alternative (rejected): static factory in `Domain` (e.g., `RecurringTaskInput.From(command, today)`). Would force the domain to reference application-layer command types, or take bare primitives and introduce a new input type — more types and churn for no behavior gain.
- Alternative (rejected): private handler method. Less churn but keeps the logic in the handler file, defeating the goal of cohesion and independent testability.

### Decision: `Parse` throws `ValidationException`, does not return a result

On failure it throws the same `ValidationException` with the same error dictionary the handler builds today. This preserves the `ApiExceptionHandler` 400 path and the MCP `Run` helper's catch (`McpTaskTools.cs:264`) untouched, and matches the domain precedent where entities throw `ValidationException` for illegal state.

- Alternative (rejected): return a `ValidationResult` and branch in each endpoint. Ripples into both HTTP and MCP call sites and splits enforcement.

### Decision: Clock stays in the handler via a `today` parameter

The recurring past-date rule needs `IClock`. `Parse(DateOnly today)` takes the boundary as a parameter; the handler computes it with its existing `Today()` helper. One-shot has no such rule, so `Parse()` is parameterless.

- Alternative (rejected): inject `IClock` into the command. Makes the command a service, no longer a plain message.

### Decision: `Parse` returns a named tuple, not a new record type

`(string Title, DateOnly StartDate, RecurrenceRule Recurrence, ReminderPolicy ReminderPolicy)` and `(string Title, DateTimeOffset DueAt, ReminderPolicy ReminderPolicy)`. Destructuring keeps the handler body readable with no new types.

- Alternative (rejected): a dedicated input record. A single-use abstraction that would need a home and a naming decision; simplicity-first.

### Decision: Add `RecurrenceUnits.TryParse` to the domain

Mirror `ReminderPolicies.TryParse` in `Domain/RecurringTaskTemplate.cs`. Replaces the handler's private `TryParseUnit`/`Set` helpers (`CreateRecurringTask.cs:96-109`), closing the asymmetry where `RecurrenceUnits` had `ToContractValue` but no `TryParse`.

## Risks / Trade-offs

- [Behavior drift in the moved validation (error keys, messages, aggregation, the `startDate != default` guard before the past-date check)] → Preserve the block verbatim inside `Parse`; the existing Core and Host tests (`RecurringTaskFeatureTests`, `ApiTests.cs:114-131`) must pass unchanged and act as the drift guard. The `startDate != default` guard is load-bearing: it prevents a failed parse from being overwritten by the past-date message.
- [Stryker mutants survive on the moved code because tests target the handler] → Handler tests exercise every `Parse` branch through the handler; if the mutation run still surfaces survivors, add direct `Parse` tests rather than weakening the handler tests.
- [Touching both create commands in one change increases diff surface] → Intentional: changing only one would leave the two creation slices philosophically inconsistent (the original smell).