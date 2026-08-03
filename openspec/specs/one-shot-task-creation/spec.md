## Purpose

Create and validate persistable active one-shot tasks through the local service API.

## Requirements

### Requirement: Create an active one-shot task
The service SHALL provide `POST /tasks/one-shot` to create a one-shot task. The request SHALL include a nonempty `title`, a `due_at` ISO-8601 date-time with an explicit UTC offset, and an explicit `reminder_policy` of `none`, `once`, or `weekly-until-done`.

When creation succeeds, the service SHALL persist a task with a stable service-assigned numeric `id`, `type` of `one-shot`, `status` of `active`, and creation and update timestamps. It SHALL return `201 Created` with the created task representation.

#### Scenario: Create a task with a due timestamp
- **WHEN** a client posts a nonempty title, an offset-qualified due timestamp, and `weekly-until-done` as the reminder policy
- **THEN** the service persists an active one-shot task and returns `201 Created` with its assigned numeric id and submitted schedule values

#### Scenario: Reject an incomplete creation request
- **WHEN** a client omits the title, due timestamp, or reminder policy from a one-shot task creation request
- **THEN** the service returns a structured JSON validation error and does not persist a task

### Requirement: Validate one-shot task schedule values
The service SHALL reject a one-shot task creation request whose due timestamp lacks an explicit UTC offset or whose reminder policy is not one of the supported values. Rejection SHALL use a structured JSON validation error and SHALL not persist a task.

#### Scenario: Reject a timestamp without an offset
- **WHEN** a client posts a one-shot task whose `due_at` value has no UTC offset
- **THEN** the service returns a structured JSON validation error for `due_at` and does not persist a task

#### Scenario: Reject an unsupported reminder policy
- **WHEN** a client posts a one-shot task with a reminder policy outside the supported enumeration
- **THEN** the service returns a structured JSON validation error for `reminder_policy` and does not persist a task
