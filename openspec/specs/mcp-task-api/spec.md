## Purpose

Expose Nagger task operations to MCP-compatible clients. The MCP surface is an independent, LLM-tuned projection of the Core task domain whose representations need not mirror the REST API field-for-field.

## Requirements

### Requirement: Provide a streamable-HTTP MCP endpoint
The service SHALL host a Model Context Protocol server using the streamable-HTTP transport in the existing Nagger host process. The MCP endpoint SHALL be available to MCP-compatible clients without replacing or changing the existing REST API.

#### Scenario: Connect an MCP client
- **WHEN** an MCP-compatible client initializes a streamable-HTTP session at the configured MCP endpoint
- **THEN** the service completes MCP initialization and advertises the Nagger task tools

### Requirement: Create one-shot tasks through MCP
The MCP server SHALL expose a `create_one_shot_task` tool accepting `title` and `dueAt`. It SHALL create a task through the existing Core create-task operation and return a structured task representation identifying the created task's id, title, schedule, lifecycle state, and timestamps.

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
The MCP server SHALL expose a read-only `list_one_shot_tasks` tool with no required arguments. The tool SHALL execute the Core open-task list query and return structured content as a JSON object whose `tasks` array contains the full task representation for each active and paused one-shot task, ordered by ascending durable task ID. The tool description SHALL identify each returned `id` as the identifier used by lifecycle tools.

#### Scenario: List open tasks through MCP
- **WHEN** a client calls `list_one_shot_tasks` after active and paused one-shot tasks have been persisted
- **THEN** the tool returns structured content containing a `tasks` array with those tasks in ascending ID order without changing task state or timestamps

#### Scenario: List when no open tasks exist through MCP
- **WHEN** a client calls `list_one_shot_tasks` and no active or paused one-shot tasks exist
- **THEN** the tool returns structured content containing an empty `tasks` array

### Requirement: List recurring task templates through MCP
The MCP server SHALL expose a read-only `list_recurring_tasks` tool with no required arguments. The tool SHALL execute the Core recurring-template list query and return structured content as a JSON object whose `tasks` array contains the full template representation for each recurring task template, ordered by ascending durable template ID. Each template representation SHALL include a `nextDueAt` timestamp equal to the due timestamp of the template's current open instance (active or paused), or `null` when the template has no open instance. The tool description SHALL identify each returned `id` as the template identifier used by the recurring lifecycle tools.

#### Scenario: List recurring templates through MCP
- **WHEN** a client calls `list_recurring_tasks` after recurring templates have been persisted
- **THEN** the tool returns structured content containing a `tasks` array with those templates in ascending ID order without changing template state or timestamps
- **AND** each template with an open instance includes a `nextDueAt` timestamp equal to that instance's due timestamp

#### Scenario: List when no recurring templates exist through MCP
- **WHEN** a client calls `list_recurring_tasks` and no recurring templates exist
- **THEN** the tool returns structured content containing an empty `tasks` array

#### Scenario: List a template with no open instance through MCP
- **WHEN** a client calls `list_recurring_tasks` and a persisted recurring template has no open instance
- **THEN** the tool returns the template with `nextDueAt` set to `null`

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
The MCP server SHALL expose a `get_morning_report` tool accepting a `date` in `YYYY-MM-DD` format. It SHALL execute the existing Core morning-report query and return its schema version, generation timestamp, requested date, summary, items, and shopping items.

#### Scenario: Read a morning report through MCP
- **WHEN** a client calls `get_morning_report` with a valid date
- **THEN** the server returns the report classified in the configured timezone, including its open shopping items, without changing task or shopping state

#### Scenario: Reject an invalid report date
- **WHEN** a client calls `get_morning_report` without a date or with a malformed date
- **THEN** the server returns an MCP tool error and does not modify task or shopping state

### Requirement: Sanitize unexpected MCP tool failures
The MCP server SHALL catch unexpected exceptions raised while executing a tool and return a generic tool error whose message contains no internal detail — no exception message, type name, or stack trace — rather than propagating the exception to the client. The server SHALL log the failure server-side.

#### Scenario: Unexpected failure inside an MCP tool
- **WHEN** a tool invocation fails with an exception other than a validation error or a not-found error
- **THEN** the server returns a generic tool error containing no exception message, type name, or stack trace, and logs the failure server-side

### Requirement: Add shopping items through MCP
The MCP server SHALL expose an `add_shopping_item` tool accepting a `name`. It SHALL execute the Core add-shopping-item operation and return a structured shopping item representation identifying the item's `id` and `name`. Adding SHALL be idempotent by name, matched case-insensitively and ignoring surrounding whitespace.

#### Scenario: Add a shopping item through MCP
- **WHEN** a client calls `add_shopping_item` with a nonempty `name` not already on the list
- **THEN** the server persists the item and returns its `id` and `name`

#### Scenario: Add is idempotent through MCP
- **WHEN** a client calls `add_shopping_item` with a `name` already on the list, differing only in letter case or surrounding whitespace
- **THEN** the server returns the existing item and does not create a duplicate

#### Scenario: Reject an invalid shopping item through MCP
- **WHEN** a client calls `add_shopping_item` with a missing or whitespace-only `name`
- **THEN** the server returns an MCP tool error and does not persist an item

### Requirement: Remove shopping items through MCP
The MCP server SHALL expose a `remove_shopping_item` tool accepting a `name`. It SHALL execute the Core remove-shopping-item operation and remove the open item whose name matches case-insensitively and ignoring surrounding whitespace. Removing SHALL be idempotent: a name that is not on the list SHALL NOT produce an error.

#### Scenario: Remove a shopping item through MCP
- **WHEN** a client calls `remove_shopping_item` with a `name` that is on the list
- **THEN** the server removes the matching item and returns a structured result

#### Scenario: Remove an absent name through MCP
- **WHEN** a client calls `remove_shopping_item` with a `name` that is not on the list
- **THEN** the server returns without error and leaves the list unchanged

### Requirement: List shopping items through MCP
The MCP server SHALL expose a read-only `list_shopping_items` tool with no required arguments. It SHALL execute the Core shopping-item list query and return structured content as a JSON object whose `shopping` array contains the item representation for each open shopping item, ordered by ascending durable item ID.

#### Scenario: List shopping items through MCP
- **WHEN** a client calls `list_shopping_items` after shopping items have been persisted
- **THEN** the tool returns structured content containing a `shopping` array with those items in ascending ID order without changing item state

#### Scenario: List when no shopping items exist through MCP
- **WHEN** a client calls `list_shopping_items` and no shopping items exist
- **THEN** the tool returns structured content containing an empty `shopping` array
