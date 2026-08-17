## MODIFIED Requirements

### Requirement: Produce a versioned morning task report
The service SHALL provide `GET /reports/morning?date=YYYY-MM-DD`. For a valid requested date, it SHALL return JSON with `schemaVersion` of `"3"`, `generatedAt`, `date`, a task summary with counts for `dueToday`, `overdue`, and `upcoming`, and task item detail for active tasks that are overdue, due today, or due within the inclusive seven-calendar-day window after the requested date. Each task item SHALL use camelCase fields including `type`, `dueAt`, `dueState`, `daysOverdue`, `daysUntilDue`, and `reminderPolicy`.

The `type` field SHALL be `one-shot` for one-shot tasks and `recurring` for recurring tasks. A recurring item SHALL use the recurring task template id as its `id` and SHALL report the due timestamp of its current active instance.

The service SHALL use its configured IANA timezone to interpret the requested report date and the local calendar date of each task due timestamp. It SHALL classify an active task as `overdue` when its due date precedes the requested date, `due_today` when the dates are equal, and `upcoming` when its due date follows the requested date. It SHALL include and count an upcoming task only when its local due date is no more than seven calendar days after the requested date; it SHALL exclude later active tasks from the report summary and item detail.

For an overdue item, `daysOverdue` SHALL contain the positive local calendar-day difference and `daysUntilDue` SHALL be null. For a due-today item, both fields SHALL be null. For an upcoming item, `daysUntilDue` SHALL contain a value from 1 through 7 and `daysOverdue` SHALL be null.

#### Scenario: Report a due-today task
- **WHEN** an active one-shot task has a due timestamp whose calendar date in the configured timezone equals the requested report date
- **THEN** the report includes the task with `type` of `one-shot`, `dueState` of `due_today`, null `daysOverdue` and `daysUntilDue`, and increments the `dueToday` summary count

#### Scenario: Report an overdue task until completion
- **WHEN** an active one-shot task has a due timestamp whose calendar date in the configured timezone precedes the requested report date
- **THEN** the report includes the task with `type` of `one-shot`, `dueState` of `overdue`, its positive `daysOverdue` value, null `daysUntilDue`, and increments the overdue summary count

#### Scenario: Report a task at the seven-day visibility boundary
- **WHEN** an active one-shot task has a due timestamp whose calendar date in the configured timezone is exactly seven calendar days after the requested report date
- **THEN** the report includes the task with `type` of `one-shot`, `dueState` of `upcoming`, `daysUntilDue` of 7, null `daysOverdue`, and increments the upcoming summary count

#### Scenario: Exclude a task beyond the seven-day visibility window
- **WHEN** an active one-shot task has a due timestamp whose calendar date in the configured timezone is more than seven calendar days after the requested report date
- **THEN** the report does not include or count that task

#### Scenario: Report a recurring task under its template id
- **WHEN** an active recurring task template has a current active instance whose due timestamp falls within the report window
- **THEN** the report includes the task with `type` of `recurring` and the template id, and increments the corresponding summary count
