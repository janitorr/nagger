## MODIFIED Requirements

### Requirement: Complete an active one-shot task
The service SHALL provide `POST /tasks/{id}/complete` to complete an active one-shot task. On success, it SHALL set the task status to `done`, set `completedAt` and `updatedAt` to the supplied current timestamp, retain its other task data, and return `200 OK` with the updated camelCase task representation.

If the completed task is a recurring-generated instance (has a recurring task template), the service SHALL additionally create a new one-shot task instance with a due date calculated as the completion date plus the template's recurrence interval, and the new instance SHALL have the same title and reminder policy as the template.

#### Scenario: Complete an active task
- **WHEN** a client posts to the complete endpoint for an active one-shot task
- **THEN** the service returns the task with status `done`, non-null `completedAt`, and an `updatedAt` value equal to `completedAt`

#### Scenario: Complete a recurring-generated instance creates next instance
- **WHEN** a client posts to the complete endpoint for an active one-shot task that was generated from a recurring template
- **THEN** the service marks the instance as done and creates a new one-shot task instance with due date equal to completion date plus the template's recurrence interval
