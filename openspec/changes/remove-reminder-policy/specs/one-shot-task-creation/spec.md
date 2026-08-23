## MODIFIED Requirements

### Requirement: Create an active one-shot task
The service SHALL provide `POST /tasks/one-shot` to create a one-shot task. The request SHALL include a nonempty `title` and a `dueAt` ISO-8601 date-time with an explicit UTC offset.

When creation succeeds, the service SHALL persist a task with a stable service-assigned numeric `id`, `type` of `one-shot`, `status` of `active`, and creation and update timestamps. It SHALL return `201 Created` with a camelCase task representation, including `dueAt`, `createdAt`, `updatedAt`, `completedAt`, and `cancelledAt`.

#### Scenario: Create a task with a due timestamp
- **WHEN** a client posts a nonempty title and an offset-qualified `dueAt`
- **THEN** the service persists an active one-shot task and returns `201 Created` with its assigned numeric id and submitted schedule values in camelCase fields

#### Scenario: Reject an incomplete creation request
- **WHEN** a client omits the title or due timestamp from a one-shot task creation request
- **THEN** the service returns a structured JSON validation error with camelCase field keys and does not persist a task

## REMOVED Requirements

### Requirement: Validate one-shot task schedule values
**Reason**: `reminderPolicy` validation is removed along with the field itself; the remaining `dueAt` validation is re-expressed as a dedicated requirement.
**Migration**: Clients must stop sending `reminderPolicy`. `dueAt` offset and past-due validation are unchanged.

## ADDED Requirements

### Requirement: Validate the one-shot task due timestamp
The service SHALL reject a one-shot task creation request whose `dueAt` timestamp lacks an explicit UTC offset or whose `dueAt` is in the past. Rejection SHALL use a structured JSON validation error with camelCase field keys and SHALL not persist a task.

#### Scenario: Reject a timestamp without an offset
- **WHEN** a client posts a one-shot task whose `dueAt` value has no UTC offset
- **THEN** the service returns a structured validation error for `dueAt` and does not persist a task

#### Scenario: Reject a due timestamp in the past
- **WHEN** a client posts a one-shot task whose `dueAt` timestamp is earlier than the current time
- **THEN** the service returns a structured validation error for `dueAt` and does not persist a task
