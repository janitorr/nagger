## MODIFIED Requirements

### Requirement: Validate recurring task template values
The service SHALL reject a recurring task creation request whose `startDate` is not in YYYY-MM-DD format, whose `startDate` is before today, whose `recurrence.every` is not a positive integer, or whose `recurrence.unit` is not days, weeks, or months. "Today" SHALL be computed in the configured IANA timezone, not UTC, and a `startDate` equal to today SHALL be accepted.

#### Scenario: Reject an invalid start date format
- **WHEN** a client posts a recurring task with a startDate value not in YYYY-MM-DD format
- **THEN** the service returns a structured validation error for startDate and does not persist a template or instance

#### Scenario: Reject a start date in the past
- **WHEN** a client posts a recurring task with a startDate earlier than today in the configured timezone
- **THEN** the service returns a structured validation error for startDate and does not persist a template or instance

#### Scenario: Accept a start date of today
- **WHEN** a client posts a recurring task with a startDate equal to today in the configured timezone
- **THEN** the service persists the template and its first instance due on that start date

#### Scenario: Reject an unsupported recurrence unit
- **WHEN** a client posts a recurring task with a recurrence unit outside days, weeks, or months
- **THEN** the service returns a structured validation error for recurrence.unit and does not persist a template or instance
