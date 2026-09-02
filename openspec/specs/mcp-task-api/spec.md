## Purpose

Expose Nagger task operations to MCP-compatible clients.

## Requirements

### Requirement: Provide a streamable-HTTP MCP endpoint
The service SHALL host a Model Context Protocol server using the streamable-HTTP transport in the existing Nagger host process. The MCP endpoint SHALL be available to MCP-compatible clients without replacing or changing the existing REST API.

#### Scenario: Connect an MCP client
- **WHEN** an MCP-compatible client initializes a streamable-HTTP session at the configured MCP endpoint
- **THEN** the service completes MCP initialization and advertises the Nagger task tools

### Requirement: Create one-shot tasks through MCP
The MCP server SHALL expose a `create_one_shot_task` tool accepting `title` and `dueAt`. It SHALL create a task through the existing Core create-task operation and return a structured task representation containing the same task fields and values as the REST task representation.

#### Scenario: Create a task through MCP
- **WHEN** a client calls `create_one_shot_task` with a nonempty title and an offset-qualified due timestamp
- **THEN** the server persists an active one-shot task and returns its assigned id, schedule, lifecycle state, and timestamps

#### Scenario: Reject an invalid MCP task
- **WHEN** a client calls `create_one_shot_task` with a missing or invalid required value
- **THEN** the server returns an MCP tool error identifying the invalid input and does not persist a task

### Requirement: Manage one-shot task lifecycle through MCP
The MCP server SHALL expose `complete_one_shot_task`, `pause_one_shot_task`, `resume_one_shot_task`, and `cancel_one_shot_task` tools, each accepting a task `id`. Each tool SHALL execute its corresponding existing Core lifecycle operation and return the resulting structured task representation.

#### Scenario: Complete a task through MCP
- **WHEN** a client calls `complete_one_shot_task` for an active task id
- **THEN** the server returns the task with status `done` and a non-null `completedAt` timestamp

#### Scenario: Reject an invalid lifecycle command
- **WHEN** a client calls a lifecycle tool for an unknown task or a task in a state that cannot accept that action
- **THEN** the server returns an MCP tool error and leaves persisted task state unchanged

### Requirement: List open one-shot tasks through MCP
The MCP server SHALL expose a read-only `list_one_shot_tasks` tool with no required arguments. The tool SHALL execute the Core open-task list query and return structured content as a JSON object whose `tasks` array contains the established full task representation for each active and paused one-shot task, ordered by ascending durable task ID. The tool description SHALL identify each returned `id` as the identifier used by lifecycle tools.

#### Scenario: List open tasks through MCP
- **WHEN** a client calls `list_one_shot_tasks` after active and paused one-shot tasks have been persisted
- **THEN** the tool returns structured content containing a `tasks` array with those tasks in ascending ID order without changing task state or timestamps

#### Scenario: List when no open tasks exist through MCP
- **WHEN** a client calls `list_one_shot_tasks` and no active or paused one-shot tasks exist
- **THEN** the tool returns structured content containing an empty `tasks` array

### Requirement: List recurring task templates through MCP
The MCP server SHALL expose a read-only `list_recurring_tasks` tool with no required arguments. The tool SHALL execute the Core recurring-template list query and return structured content as a JSON object whose `tasks` array contains the established full template representation for each recurring task template, ordered by ascending durable template ID. The tool description SHALL identify each returned `id` as the template identifier used by the recurring lifecycle tools.

#### Scenario: List recurring templates through MCP
- **WHEN** a client calls `list_recurring_tasks` after recurring templates have been persisted
- **THEN** the tool returns structured content containing a `tasks` array with those templates in ascending ID order without changing template state or timestamps

#### Scenario: List when no recurring templates exist through MCP
- **WHEN** a client calls `list_recurring_tasks` and no recurring templates exist
- **THEN** the tool returns structured content containing an empty `tasks` array

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

### Requirement: Read morning reports through MCP
The MCP server SHALL expose a `get_morning_report` tool accepting a `date` in `YYYY-MM-DD` format. It SHALL execute the existing Core morning-report query and return its schema version, generation timestamp, requested date, summary, and items.

#### Scenario: Read a morning report through MCP
- **WHEN** a client calls `get_morning_report` with a valid date
- **THEN** the server returns the report classified in the configured timezone without changing task state

#### Scenario: Reject an invalid report date
- **WHEN** a client calls `get_morning_report` without a date or with a malformed date
- **THEN** the server returns an MCP tool error and does not modify task state
