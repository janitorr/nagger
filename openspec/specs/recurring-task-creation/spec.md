## Purpose

Enable users to create recurring task templates that automatically generate recurring task instances for repeated obligations.

## Requirements

### Requirement: Create recurring task template
The service SHALL provide `POST /tasks/recurring` to create a recurring task template. The request SHALL include a nonempty `title`, a `startDate` in YYYY-MM-DD format, a `recurrence` object with `every` (positive integer) and `unit` (days, weeks, or months), and an explicit `reminderPolicy` of `none`, `once`, or `weekly-until-done`.

When creation succeeds, the service SHALL persist a template and create the first recurring task instance with a due date equal to the start date. It SHALL return `201 Created` with the template representation.

#### Scenario: Create a weekly recurring task template
- **WHEN** a client posts a valid title, startDate of 2026-08-06, recurrence of every 1 week, and reminderPolicy of once
- **THEN** the service persists a recurring template and creates a recurring task instance due on 2026-08-06

#### Scenario: Reject an incomplete recurring task creation request
- **WHEN** a client omits the title, startDate, recurrence, or reminderPolicy from a recurring task creation request
- **THEN** the service returns a structured JSON validation error and does not persist a template or instance

### Requirement: Validate recurring task template values
The service SHALL reject a recurring task creation request whose `startDate` is not in YYYY-MM-DD format, whose `recurrence.every` is not a positive integer, whose `recurrence.unit` is not days, weeks, or months, or whose `reminderPolicy` is not one of the supported values.

#### Scenario: Reject an invalid start date format
- **WHEN** a client posts a recurring task with a startDate value not in YYYY-MM-DD format
- **THEN** the service returns a structured validation error for startDate and does not persist a template or instance

#### Scenario: Reject an unsupported recurrence unit
- **WHEN** a client posts a recurring task with a recurrence unit outside days, weeks, or months
- **THEN** the service returns a structured validation error for recurrence.unit and does not persist a template or instance