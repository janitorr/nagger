## MODIFIED Requirements

### Requirement: Produce a versioned morning task report
The service SHALL provide `GET /reports/morning?date=YYYY-MM-DD`. For a valid requested date, it SHALL return JSON containing `schemaVersion`, `generatedAt`, `date`, a task summary with counts for `dueToday`, `overdue`, and `upcoming`, and task item detail for active tasks due today or overdue. Each task item SHALL use camelCase fields, including `dueAt`, `dueState`, `daysOverdue`, and `reminderPolicy`.

The service SHALL use its configured IANA timezone to interpret the requested report date and the local calendar date of each task due timestamp. It SHALL classify an active task as `overdue` when its due date precedes the requested date, `due_today` when the dates are equal, and `upcoming` when its due date follows the requested date.

#### Scenario: Report a due-today task
- **WHEN** an active one-shot task has a due timestamp whose calendar date in the configured timezone equals the requested report date
- **THEN** the report includes the task with `dueState` of `due_today` and increments the `dueToday` summary count

#### Scenario: Report an overdue task
- **WHEN** an active one-shot task has a due timestamp whose calendar date in the configured timezone precedes the requested report date
- **THEN** the report includes the task with `dueState` of `overdue`, includes its `daysOverdue` value, and increments the overdue summary count

#### Scenario: Count an upcoming task without including item detail
- **WHEN** an active one-shot task has a due timestamp whose calendar date in the configured timezone follows the requested report date
- **THEN** the report increments the upcoming summary count and does not include the task in report item detail
