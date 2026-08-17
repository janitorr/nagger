## MODIFIED Requirements

### Requirement: Complete an active one-shot task
The service SHALL provide `POST /tasks/{id}/complete` to complete an active one-shot task. On success, it SHALL set the task status to `done`, set `completedAt` and `updatedAt` to the supplied current timestamp, retain its other task data, and return `200 OK` with the updated camelCase task representation.

#### Scenario: Complete an active task
- **WHEN** a client posts to the complete endpoint for an active one-shot task
- **THEN** the service returns the task with status `done`, non-null `completedAt`, and an `updatedAt` value equal to `completedAt`
