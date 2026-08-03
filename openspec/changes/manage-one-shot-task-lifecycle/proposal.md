## Why

Created one-shot tasks currently remain active indefinitely, so the service cannot reflect that work was completed, deliberately paused, or abandoned. Adding the explicit lifecycle now closes the core task loop and keeps Morning Digest output aligned with the user's current obligations before recurrence and reminder delivery add further scheduling complexity.

## What Changes

- Add persisted one-shot task statuses: `active`, `paused`, `done`, and `cancelled`.
- Add strict Core lifecycle commands and HTTP endpoints to complete, pause, resume, and cancel a one-shot task.
- Track completion and cancellation timestamps, and update `updated_at` for every valid state transition.
- Return the updated one-shot task from lifecycle commands and distinguish an unknown task from an invalid transition.
- Reshape the disposable initial SQLite schema and migration to persist lifecycle state, and ensure non-active tasks are excluded from Morning Report results.

## Capabilities

### New Capabilities
- `one-shot-task-lifecycle`: Manage one-shot task status transitions and their timestamped outcomes through the local service API.

### Modified Capabilities

None.

## Impact

- Extends the Core task model and `ITaskStore` persistence port with task lookup and state updates.
- Replaces the initial EF Core migration and SQLite `one_shot_tasks` mapping with the lifecycle-aware schema; no production data migration is required.
- Adds `POST /tasks/{id}/complete`, `/pause`, `/resume`, and `/cancel` endpoints and expands the task response representation.
- Adds Core lifecycle tests and Host integration coverage for transitions, persistence, reporting exclusion, validation, and missing task responses.
