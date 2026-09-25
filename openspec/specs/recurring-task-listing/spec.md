## Purpose

Enable users to discover and inspect recurring task templates.

## Requirements

### Requirement: List recurring task templates
The service SHALL provide `GET /tasks/recurring` to list all recurring task templates. It SHALL return `200 OK` with a JSON array of template representations, ordered by ascending template ID. Each template representation SHALL include a `nextDueAt` timestamp equal to the due timestamp of the template's current open instance (active or paused), or `null` when the template has no open instance.

#### Scenario: List all recurring templates
- **WHEN** a client requests GET /tasks/recurring
- **THEN** the service returns 200 OK with an array of all recurring task templates in ascending ID order
- **AND** each template with an open instance includes a `nextDueAt` timestamp equal to that instance's due timestamp

#### Scenario: List when no recurring templates exist
- **WHEN** a client requests GET /tasks/recurring and no recurring templates exist
- **THEN** the service returns 200 OK with an empty JSON array

#### Scenario: List a template with no open instance
- **WHEN** a client requests GET /tasks/recurring and a persisted recurring template has no open instance
- **THEN** the service returns the template with `nextDueAt` set to `null`