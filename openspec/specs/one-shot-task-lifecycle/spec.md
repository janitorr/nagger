## Purpose

Define the persisted lifecycle and report behavior for one-shot tasks.

## Requirements

### Requirement: Persist one-shot task lifecycle state
The service SHALL persist each one-shot task with a status of `active`, `paused`, `done`, or `cancelled`. A newly created one-shot task SHALL have status `active`. The service SHALL retain one-shot task records after completion or cancellation, including nullable completion and cancellation timestamps represented as `completedAt` and `cancelledAt` in JSON task representations.

#### Scenario: Create a task with lifecycle fields
- **WHEN** a client creates a one-shot task
- **THEN** the service persists it with status `active` and returns null `completedAt` and `cancelledAt` fields

### Requirement: Complete an active one-shot task
The service SHALL provide `POST /tasks/{id}/complete` to complete an active one-shot task. On success, it SHALL set the task status to `done`, set `completedAt` and `updatedAt` to the supplied current timestamp, retain its other task data, and return `200 OK` with the updated camelCase task representation.

#### Scenario: Complete an active task
- **WHEN** a client posts to the complete endpoint for an active one-shot task
- **THEN** the service returns the task with status `done`, non-null `completedAt`, and an `updatedAt` value equal to `completedAt`

### Requirement: Pause and resume a one-shot task
The service SHALL provide `POST /tasks/{id}/pause` for an active one-shot task and `POST /tasks/{id}/resume` for a paused one-shot task. A successful pause SHALL set status to `paused` and update `updatedAt`; a successful resume SHALL set status to `active` and update `updatedAt`. Each endpoint SHALL return `200 OK` with the updated camelCase task representation and SHALL NOT set `completedAt` or `cancelledAt`.

#### Scenario: Pause an active task
- **WHEN** a client posts to the pause endpoint for an active one-shot task
- **THEN** the service returns the task with status `paused` and a later `updatedAt` value

#### Scenario: Resume a paused task
- **WHEN** a client posts to the resume endpoint for a paused one-shot task
- **THEN** the service returns the task with status `active` and leaves `completedAt` and `cancelledAt` null

### Requirement: Cancel an active or paused one-shot task
The service SHALL provide `POST /tasks/{id}/cancel` to cancel an active or paused one-shot task. On success, it SHALL set the task status to `cancelled`, set `cancelledAt` and `updatedAt` to the supplied current timestamp, retain its other task data, and return `200 OK` with the updated camelCase task representation.

#### Scenario: Cancel a paused task
- **WHEN** a client posts to the cancel endpoint for a paused one-shot task
- **THEN** the service returns the task with status `cancelled`, non-null `cancelledAt`, and an `updatedAt` value equal to `cancelledAt`

### Requirement: Reject invalid lifecycle transitions
The service SHALL accept only these one-shot task transitions: `active` to `paused`, `done`, or `cancelled`; and `paused` to `active` or `cancelled`. It SHALL reject all other lifecycle commands with a `400 Bad Request` structured JSON validation error and SHALL leave the task unchanged. A `done` or `cancelled` task SHALL be terminal.

#### Scenario: Reject completion of a paused task
- **WHEN** a client posts to the complete endpoint for a paused one-shot task
- **THEN** the service returns a structured validation error and the task remains paused

#### Scenario: Reject a transition from a terminal task
- **WHEN** a client posts any lifecycle command for a done or cancelled one-shot task
- **THEN** the service returns a structured validation error and retains the terminal task state and timestamps

### Requirement: Report a missing lifecycle task distinctly
The service SHALL return `404 Not Found` when a lifecycle endpoint addresses a one-shot task id that does not exist, and SHALL not create or update a task.

#### Scenario: Address an unknown task
- **WHEN** a client posts to a lifecycle endpoint using an id with no persisted one-shot task
- **THEN** the service returns `404 Not Found` and the task store remains unchanged

### Requirement: Exclude non-active one-shot tasks from morning reports
The Morning Report SHALL include and count only active one-shot tasks. Paused, done, and cancelled one-shot tasks SHALL be excluded from due-today, overdue, and upcoming counts and from detailed report items.

#### Scenario: Read a report after completing a due task
- **WHEN** an active one-shot task due on the requested report date is completed before the report is read
- **THEN** the report does not include or count that task
