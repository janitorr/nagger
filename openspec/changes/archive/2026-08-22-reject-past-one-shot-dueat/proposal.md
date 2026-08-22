## Why

One-shot task creation accepts a `dueAt` timestamp in the past and returns `201 Created`, while recurring task creation already rejects a past `startDate`. A brand-new task that is already overdue is almost always a mistake (timezone slip, typo, wrong year), so the two creation paths should agree on the same principle.

## What Changes

- `POST /tasks/one-shot` rejects a `dueAt` timestamp that is in the past, returning `400 Bad Request` with a structured validation error on the `dueAt` field.
- The past check compares the `dueAt` instant against the current instant from the `IClock` port (`dueAt < clock.UtcNow`), not a calendar-date comparison, so a timestamp earlier today is also rejected.
- A task that is valid at creation and later ages into overdue is unaffected — the restriction applies only to creation, not to existing or aging tasks.
- USAGE.md documents the new one-shot `dueAt` past-date rule.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `one-shot-task-creation`: the service now rejects a one-shot task creation request whose `dueAt` is in the past, in addition to the existing format and offset validation.

## Impact

- `src/Nagger.Core/Tasks/CreateOneShotTask.cs` — add the past-`dueAt` check to `CreateOneShotTaskCommand.Parse`, passing the current instant from `IClock` into it.
- `USAGE.md` — document the `dueAt` past-date rule in the one-shot field table.
- No schema, endpoint contract, persistence, or migration changes; validation flows through the existing `ValidationException` → `400` path (REST) and `ValidationException` catch (MCP).
