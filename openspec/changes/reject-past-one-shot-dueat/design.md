## Context

`CreateOneShotTaskCommand.Parse()` (in `src/Nagger.Core/Tasks/CreateOneShotTask.cs`) validates the `dueAt` format and offset but never compares it against the current time. `CreateRecurringTaskCommand.Parse(DateOnly today)` already rejects a past `startDate` by comparing the parsed date against `today`, which the handler computes from `IClock`. The one-shot handler already injects `IClock` and already computes `now = clock.UtcNow` for the task's `CreatedAt`/`UpdatedAt`, so the clock is available at the enforcement point. See proposal.md - Why for motivation.

## Goals / Non-Goals

**Goals:**

- Reject a one-shot `dueAt` strictly in the past at creation, in the same `Parse` validation pass that already produces the `ValidationException` error dictionary.
- Preserve the exact `ValidationException` error dictionary shape (keys, messages, aggregation) and the "throw before any persistence" behavior.
- Mirror the recurring rule's structure and wording so the two creation paths stay consistent.

**Non-Goals:**

- Do not add any restriction on existing tasks or tasks that age into overdue; only creation is guarded.
- Do not touch recurring `startDate` validation (already correct).
- No schema, endpoint, persistence, or migration changes.

## Decisions

1. **Instant comparison: `dueAt < clock.UtcNow`.** The `dueAt` is a `DateTimeOffset` (an instant), so "in the past" means "before now." This is timezone-independent — the offset is self-contained — and requires no date-boundary math in the configured timezone.
   - *Alternative considered:* mirror recurring exactly by converting `dueAt` to the configured timezone and rejecting when its local calendar date precedes today. Rejected: it would allow a `dueAt` earlier the same day, which is still a missed obligation, and it drags timezone conversion into what is otherwise a single instant comparison.

2. **Pass the current instant into `Parse`.** Change the signature to `Parse(DateTimeOffset now)` and have the handler pass `clock.UtcNow`, exactly as `CreateRecurringTaskCommand.Parse(DateOnly today)` receives `today` from its handler. This keeps the comparison clock-driven (Core never reads the system clock directly) and keeps `Parse` side-effect-free and unit-testable.

3. **Guard `dueAt != default`.** Mirror the recurring `startDate != default` guard so a format/offset failure does not also emit the past-date error (a failed parse yields `default(DateTimeOffset)`, which would otherwise look "in the past").

4. **Error key and wording.** Reject with `errors["dueAt"] = ["Due timestamp cannot be in the past."]`, mirroring the recurring `"Start date cannot be in the past."` on the `startDate` key. This flows through the existing `ApiExceptionHandler` → `400` path and the MCP `Run` helper's `ValidationException` catch without any change to those layers.

5. **Replace the custom `IClock` port with the BCL `System.TimeProvider` abstraction.** `TimeProvider` is a .NET runtime type (not ASP.NET Core), so Core may reference it without violating the no-ASP.NET boundary. It supplies `GetUtcNow()`; the configurable timezone is preserved by subclassing `TimeProvider` in Host and overriding `LocalTimeZone` from `Nagger:TimeZone`. This removes the bespoke clock port in favor of the framework-standard abstraction, lets the Host test factory inject a deterministic fake `TimeProvider`, and lets Core tests subclass a fake instead of a custom `TestClock`.
   - *Mapping:* `clock.UtcNow` → `timeProvider.GetUtcNow()`; `clock.TimeZone` → `timeProvider.LocalTimeZone`.
   - *Test seam:* Host tests register a fake `TimeProvider` pinned to `2026-08-03T06:00:00Z` (Europe/Helsinki), the same scenario instant the Core tests already use. Existing one-shot fixtures are all creation-valid at that instant; the one "Overdue" fixture moves to `dueAt == now` so it is valid at creation yet still overdue for the `2026-08-04` report date.

## Risks / Trade-offs

- [A `dueAt` earlier the same calendar day is now rejected] → Intended: a timestamp before "now" is a missed obligation, matching the proposal's rationale. Surface via the new scenario in the spec.
- [Two errors could be reported for one bad `dueAt` (format + past)] → Prevented by the `dueAt != default` guard (Decision 3).
- [MCP `create_one_shot_task` also inherits the rule] → The shared command/handler is the single enforcement point, so REST and MCP stay in sync; no extra code, only optional test coverage.

## Migration Plan

None. No data model, schema, or persisted-data change; the rule applies only to newly created tasks.
