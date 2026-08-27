## MODIFIED Requirements

### Requirement: Create recurring task template
The service SHALL provide `POST /tasks/recurring` to create a recurring task template. The request SHALL include a nonempty `title`, a `startDate` in YYYY-MM-DD format, and a `recurrence` object with `every` (positive integer) and `unit` (days, weeks, or months).

When creation succeeds, the service SHALL persist a template and create the first recurring task instance with a due date equal to the start date. It SHALL return `201 Created` with an envelope containing the created template representation under `template` and the newly created first instance representation under `firstInstance`, where `firstInstance` includes the instance `id`, `title`, and `dueAt`.

#### Scenario: Create a weekly recurring task template
- **WHEN** a client posts a valid title, startDate of 2026-08-06, and recurrence of every 1 week
- **THEN** the service persists a recurring template, creates a recurring task instance due on 2026-08-06, and returns the template under `template` and that first instance under `firstInstance`

#### Scenario: Reject an incomplete recurring task creation request
- **WHEN** a client omits the title, startDate, or recurrence from a recurring task creation request
- **THEN** the service returns a structured JSON validation error and does not persist a template or instance
