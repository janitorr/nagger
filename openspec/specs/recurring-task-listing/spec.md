## Purpose

Enable users to discover and inspect recurring task templates.

## Requirements

### Requirement: List recurring task templates
The service SHALL provide `GET /tasks/recurring` to list all recurring task templates. It SHALL return `200 OK` with a JSON array of template representations, ordered by ascending template ID.

#### Scenario: List all recurring templates
- **WHEN** a client requests GET /tasks/recurring
- **THEN** the service returns 200 OK with an array of all recurring task templates in ascending ID order

#### Scenario: List when no recurring templates exist
- **WHEN** a client requests GET /tasks/recurring and no recurring templates exist
- **THEN** the service returns 200 OK with an empty JSON array