## MODIFIED Requirements

### Requirement: List open one-shot tasks
The service SHALL provide `GET /tasks/one-shot` with no required query parameters. It SHALL return `200 OK` with a JSON array of the established camelCase one-shot task representation for every persisted task whose status is `active` or `paused`, ordered by ascending durable task ID. The response SHALL exclude tasks whose status is `done` or `cancelled`.

#### Scenario: List active and paused tasks
- **WHEN** a client requests `GET /tasks/one-shot` after active, paused, done, and cancelled one-shot tasks have been persisted
- **THEN** the response contains only the active and paused tasks in ascending ID order, with each task's ID, title, type, status, due timestamp, and lifecycle timestamps

#### Scenario: List when no open tasks exist
- **WHEN** a client requests `GET /tasks/one-shot` and no active or paused one-shot tasks exist
- **THEN** the response is `200 OK` with an empty JSON array
