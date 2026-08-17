## ADDED Requirements

### Requirement: Report a missing recurring task template distinctly
The service SHALL return `404 Not Found` when a recurring lifecycle endpoint addresses a recurring task template id that does not exist, and SHALL not create or update a template or instance.

#### Scenario: Address an unknown recurring task template
- **WHEN** a client posts to a recurring lifecycle endpoint using an id with no persisted recurring task template
- **THEN** the service returns `404 Not Found` and no template or instance changes

## MODIFIED Requirements

### Requirement: Complete a recurring task instance
The service SHALL provide `POST /tasks/recurring/{id}/complete` to complete the current active instance of the recurring task template identified by `{id}`. On success, it SHALL mark that instance as done, calculate the next due date as the completion date plus the recurrence interval, and create a new recurring task instance with that due date. It SHALL return `200 OK` with the completed instance representation.

#### Scenario: Complete a recurring task instance and generate next
- **WHEN** a client posts to the complete endpoint for a recurring task template that has a current active instance
- **THEN** the service marks that instance as done, creates a new recurring task instance with due date equal to completion date plus recurrence, and returns the completed instance

#### Scenario: Reject completion when no active instance exists
- **WHEN** a client posts to the complete endpoint for a recurring task template that has no active instance
- **THEN** the service returns a structured validation error and leaves task state unchanged
