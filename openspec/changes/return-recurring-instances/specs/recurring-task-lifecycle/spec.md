## MODIFIED Requirements

### Requirement: Complete a recurring task instance
The service SHALL provide `POST /tasks/recurring/{id}/complete` to complete the current active instance of the recurring task template identified by `{id}`. On success, it SHALL mark that instance as done, calculate the next due date as the completion date plus the recurrence interval, and create a new recurring task instance with that due date. It SHALL return `200 OK` with an envelope containing the completed instance representation under `completedInstance` and the newly created next instance representation under `nextInstance`, where `nextInstance` includes the instance `id`, `title`, and `dueAt`.

#### Scenario: Complete a recurring task instance and generate next
- **WHEN** a client posts to the complete endpoint for a recurring task template that has a current active instance
- **THEN** the service marks that instance as done, creates a new recurring task instance with due date equal to completion date plus recurrence, and returns the completed instance under `completedInstance` and the next instance under `nextInstance`

#### Scenario: Reject completion when no active instance exists
- **WHEN** a client posts to the complete endpoint for a recurring task template that has no active instance
- **THEN** the service returns a structured validation error and leaves task state unchanged
