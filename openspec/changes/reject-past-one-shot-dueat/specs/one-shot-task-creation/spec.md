## MODIFIED Requirements

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
