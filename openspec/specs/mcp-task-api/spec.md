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

### Requirement: Read morning reports through MCP
The MCP server SHALL expose a `get_morning_report` tool accepting a `date` in `YYYY-MM-DD` format. It SHALL execute the existing Core morning-report query and return its schema version, generation timestamp, requested date, summary, and items.

#### Scenario: Read a morning report through MCP
- **WHEN** a client calls `get_morning_report` with a valid date
- **THEN** the server returns the report classified in the configured timezone without changing task state

#### Scenario: Reject an invalid report date
- **WHEN** a client calls `get_morning_report` without a date or with a malformed date
- **THEN** the server returns an MCP tool error and does not modify task state
