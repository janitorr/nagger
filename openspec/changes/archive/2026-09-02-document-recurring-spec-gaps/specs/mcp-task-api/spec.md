## ADDED Requirements

### Requirement: Pause recurring tasks through MCP
The MCP server SHALL expose a `pause_recurring_task` tool accepting a template `id`. It SHALL execute the existing Core pause-recurring-task operation and return the resulting structured template representation. Pausing SHALL set the template status to paused and pause its current active instance.

#### Scenario: Pause a recurring task through MCP
- **WHEN** a client calls `pause_recurring_task` for an active recurring task template with a current active instance
- **THEN** the server sets the template status to paused, pauses its current instance, and returns the updated template representation

#### Scenario: Reject pausing a recurring task through MCP
- **WHEN** a client calls `pause_recurring_task` for an unknown template id or a template already paused
- **THEN** the server returns an MCP tool error and leaves persisted state unchanged

### Requirement: Resume recurring tasks through MCP
The MCP server SHALL expose a `resume_recurring_task` tool accepting a template `id`. It SHALL execute the existing Core resume-recurring-task operation and return the resulting structured template representation. Resuming SHALL set the template status to active and resume its current paused instance.

#### Scenario: Resume a recurring task through MCP
- **WHEN** a client calls `resume_recurring_task` for a paused recurring task template with a current paused instance
- **THEN** the server sets the template status to active, resumes its current instance, and returns the updated template representation

#### Scenario: Reject resuming a recurring task through MCP
- **WHEN** a client calls `resume_recurring_task` for an unknown template id or a template that is not paused
- **THEN** the server returns an MCP tool error and leaves persisted state unchanged

### Requirement: Cancel recurring tasks through MCP
The MCP server SHALL expose a `cancel_recurring_task` tool accepting a template `id`. It SHALL execute the existing Core cancel-recurring-task operation and return the resulting structured template representation. Cancelling SHALL set the template status to cancelled, set its cancellation timestamp, and cancel all its generated instances.

#### Scenario: Cancel a recurring task through MCP
- **WHEN** a client calls `cancel_recurring_task` for an active recurring task template
- **THEN** the server sets the template status to cancelled, sets its cancellation timestamp, cancels all its instances, and returns the updated template representation

#### Scenario: Reject cancelling a recurring task through MCP
- **WHEN** a client calls `cancel_recurring_task` for an unknown template id or a template already cancelled
- **THEN** the server returns an MCP tool error and leaves persisted state unchanged
