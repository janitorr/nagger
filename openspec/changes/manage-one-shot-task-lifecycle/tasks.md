## 1. Core Lifecycle Behavior

- [x] 1.1 Extend the one-shot task model with supported status values and nullable completion and cancellation timestamps; ensure new tasks are active.
- [x] 1.2 Extend the task persistence port with minimal lookup and state-update operations while retaining active-only report reads.
- [x] 1.3 Implement Core Mediator commands and handlers for completing, pausing, resuming, and cancelling a one-shot task, using the injected clock for transition timestamps.
- [x] 1.4 Return a distinguishable missing-task outcome and reject every invalid or terminal-state transition with the existing structured validation error mechanism.
- [x] 1.5 Add Core unit tests named `Subject_GivenCondition_WhenAction_ThenOutcome` for every allowed transition, timestamp effects, missing task, invalid transition, and terminal task behavior.

## 2. SQLite Persistence and Migration

- [x] 2.1 Add status, completed-at, and cancelled-at mappings to the SQLite task entity and filter active-task reads by persisted active status.
- [x] 2.2 Implement SQLite task lookup and update operations that preserve task identity, schedule data, and terminal timestamps.
- [x] 2.3 Reshape the initial EF Core migration and model snapshot to create required status plus nullable completion and cancellation timestamp columns.
- [x] 2.4 Add Host integration coverage proving a fresh SQLite database is created with lifecycle-aware task persistence.

## 3. HTTP API and Reporting

- [x] 3.1 Expand the shared task response representation to expose status and nullable completion and cancellation timestamps for creation and lifecycle responses.
- [x] 3.2 Map the four lifecycle POST endpoints, returning the updated representation with `200 OK`, mapping missing tasks to `404 Not Found`, and preserving structured `400` validation errors for invalid transitions.
- [x] 3.3 Add Host integration tests named `Subject_GivenCondition_WhenAction_ThenOutcome` for complete, pause, resume, cancel, missing-id, and invalid-transition HTTP outcomes.
- [x] 3.4 Add Core and Host integration tests verifying paused, done, and cancelled tasks are excluded from Morning Report counts and detailed items without report-side state changes.
- [x] 3.5 Run `dotnet test Nagger.slnx` and verify the migration and lifecycle endpoints against a local SQLite database.
