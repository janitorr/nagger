## ADDED Requirements

### Requirement: Create recurring tasks through MCP
The MCP server SHALL expose a `create_recurring_task` tool accepting `title`, `startDate`, `recurrenceEvery`, and `recurrenceUnit`. It SHALL execute the existing Core create-recurring-task operation and return structured content whose top level contains the created template representation under `template` and the newly created first instance representation under `firstInstance`. The tool description SHALL state that the response contains both the template and its first instance.

#### Scenario: Create a recurring task through MCP
- **WHEN** a client calls `create_recurring_task` with a nonempty title, a valid start date, and valid recurrence values
- **THEN** the server persists the template and its first instance and returns structured content with `template` and `firstInstance`

#### Scenario: Reject an invalid MCP recurring task
- **WHEN** a client calls `create_recurring_task` with a missing or invalid required value
- **THEN** the server returns an MCP tool error identifying the invalid input and does not persist a template or instance

### Requirement: Complete recurring tasks through MCP
The MCP server SHALL expose a `complete_recurring_task` tool accepting a template `id`. It SHALL execute the existing Core complete-recurring-task operation and return structured content whose top level contains the completed instance representation under `completedInstance` and the newly scheduled next instance representation under `nextInstance`. The tool description SHALL state that the response contains both the completed and the next instance.

#### Scenario: Complete a recurring task through MCP
- **WHEN** a client calls `complete_recurring_task` for a recurring task template with an active instance
- **THEN** the server marks the current instance done, schedules the next instance, and returns structured content with `completedInstance` and `nextInstance`

#### Scenario: Reject completing a recurring task with no active instance through MCP
- **WHEN** a client calls `complete_recurring_task` for a template with no active instance
- **THEN** the server returns an MCP tool error and leaves persisted state unchanged
