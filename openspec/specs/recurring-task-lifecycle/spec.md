## Purpose

Enable users to manage the lifecycle of recurring task templates and their generated instances.

## Requirements

### Requirement: Complete a recurring task instance
The service SHALL provide `POST /tasks/recurring/{id}/complete` to complete the current active instance of the recurring task template identified by `{id}`. On success, it SHALL mark that instance as done, calculate the next due date as the completion date plus the recurrence interval, and create a new recurring task instance with that due date. It SHALL return `200 OK` with an envelope containing the completed instance representation under `completedInstance` and the newly created next instance representation under `nextInstance`, where `nextInstance` includes the instance `id`, `title`, and `dueAt`.

#### Scenario: Complete a recurring task instance and generate next
- **WHEN** a client posts to the complete endpoint for a recurring task template that has a current active instance
- **THEN** the service marks that instance as done, creates a new recurring task instance with due date equal to completion date plus recurrence, and returns the completed instance under `completedInstance` and the next instance under `nextInstance`

#### Scenario: Reject completion when no active instance exists
- **WHEN** a client posts to the complete endpoint for a recurring task template that has no active instance
- **THEN** the service returns a structured validation error and leaves task state unchanged

### Requirement: Report a missing recurring task template distinctly
The service SHALL return `404 Not Found` when a recurring lifecycle endpoint addresses a recurring task template id that does not exist, and SHALL not create or update a template or instance.

#### Scenario: Address an unknown recurring task template
- **WHEN** a client posts to a recurring lifecycle endpoint using an id with no persisted recurring task template
- **THEN** the service returns `404 Not Found` and no template or instance changes

### Requirement: Pause a recurring task template
The service SHALL provide `POST /tasks/recurring/{id}/pause` to pause a recurring task template. On success, it SHALL set the template status to paused and pause its current active instance. It SHALL return `200 OK` with the updated template representation.

#### Scenario: Pause an active recurring template
- **WHEN** a client posts to the pause endpoint for an active recurring task template
- **THEN** the service sets template status to paused and pauses its current active instance

### Requirement: Resume a recurring task template
The service SHALL provide `POST /tasks/recurring/{id}/resume` to resume a paused recurring task template. On success, it SHALL set the template status to active and resume its current paused instance. It SHALL return `200 OK` with the updated template representation.

#### Scenario: Resume a paused recurring template
- **WHEN** a client posts to the resume endpoint for a paused recurring task template
- **THEN** the service sets template status to active and resumes its current paused instance

### Requirement: Cancel a recurring task template
The service SHALL provide `POST /tasks/recurring/{id}/cancel` to cancel a recurring task template. On success, it SHALL set the template status to cancelled, set cancelledAt timestamp, and cancel all its instances. It SHALL return `200 OK` with the updated template representation.

#### Scenario: Cancel an active recurring template
- **WHEN** a client posts to the cancel endpoint for an active recurring task template
- **THEN** the service sets template status to cancelled, sets cancelledAt, and cancels all its instances

### Requirement: Reject invalid recurring lifecycle transitions
The service SHALL reject lifecycle commands for recurring task templates that are not in a valid state for that transition.

#### Scenario: Reject pausing a paused template
- **WHEN** a client posts to the pause endpoint for a paused recurring task template
- **THEN** the service returns a structured validation error and leaves template state unchanged

#### Scenario: Reject resuming an active template
- **WHEN** a client posts to the resume endpoint for an active recurring task template
- **THEN** the service returns a structured validation error and leaves template state unchanged