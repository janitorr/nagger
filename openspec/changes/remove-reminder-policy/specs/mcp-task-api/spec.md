## MODIFIED Requirements

### Requirement: Create one-shot tasks through MCP
The MCP server SHALL expose a `create_one_shot_task` tool accepting `title` and `dueAt`. It SHALL create a task through the existing Core create-task operation and return a structured task representation containing the same task fields and values as the REST task representation.

#### Scenario: Create a task through MCP
- **WHEN** a client calls `create_one_shot_task` with a nonempty title and an offset-qualified due timestamp
- **THEN** the server persists an active one-shot task and returns its assigned id, schedule, lifecycle state, and timestamps

#### Scenario: Reject an invalid MCP task
- **WHEN** a client calls `create_one_shot_task` with a missing or invalid required value
- **THEN** the server returns an MCP tool error identifying the invalid input and does not persist a task

### Requirement: Read morning reports through MCP
The MCP server SHALL expose a `get_morning_report` tool accepting a `date` in `YYYY-MM-DD` format. It SHALL execute the existing Core morning-report query and return its schema version, generation timestamp, requested date, summary, and items.

#### Scenario: Read a morning report through MCP
- **WHEN** a client calls `get_morning_report` with a valid date
- **THEN** the server returns the report classified in the configured timezone without changing task state

#### Scenario: Reject an invalid report date
- **WHEN** a client calls `get_morning_report` without a date or with a malformed date
- **THEN** the server returns an MCP tool error and does not modify task state
