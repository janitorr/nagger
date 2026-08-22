## Purpose

Create and validate persistable active one-shot tasks through the local service API.

## Requirements

### Requirement: Create an active one-shot task
The service SHALL provide `POST /tasks/one-shot` to create a one-shot task. The request SHALL include a nonempty `title`, a `dueAt` ISO-8601 date-time with an explicit UTC offset, and an explicit `reminderPolicy` of `none`, `once`, or `weekly-until-done`.

When creation succeeds, the service SHALL persist a task with a stable service-assigned numeric `id`, `type` of `one-shot`, `status` of `active`, and creation and update timestamps. It SHALL return `201 Created` with a camelCase task representation, including `dueAt`, `reminderPolicy`, `createdAt`, `updatedAt`, `completedAt`, and `cancelledAt`.

#### Scenario: Create a task with a due timestamp
- **WHEN** a client posts a nonempty title, an offset-qualified `dueAt`, and `weekly-until-done` as the `reminderPolicy`
- **THEN** the service persists an active one-shot task and returns `201 Created` with its assigned numeric id and submitted schedule values in camelCase fields

#### Scenario: Reject an incomplete creation request
- **WHEN** a client omits the title, due timestamp, or reminder policy from a one-shot task creation request
- **THEN** the service returns a structured JSON validation error with camelCase field keys and does not persist a task

### Requirement: Validate one-shot task schedule values
The service SHALL reject a one-shot task creation request whose `dueAt` timestamp lacks an explicit UTC offset, whose `dueAt` is in the past, or whose `reminderPolicy` is not one of the supported values. Rejection SHALL use a structured JSON validation error with camelCase field keys and SHALL not persist a task.

#### Scenario: Reject a timestamp without an offset
- **WHEN** a client posts a one-shot task whose `dueAt` value has no UTC offset
- **THEN** the service returns a structured validation error for `dueAt` and does not persist a task

#### Scenario: Reject a due timestamp in the past
- **WHEN** a client posts a one-shot task whose `dueAt` timestamp is earlier than the current time
- **THEN** the service returns a structured validation error for `dueAt` and does not persist a task

#### Scenario: Reject an unsupported reminder policy
- **WHEN** a client posts a one-shot task with a `reminderPolicy` value outside the supported enumeration
- **THEN** the service returns a structured validation error for `reminderPolicy` and does not persist a task
