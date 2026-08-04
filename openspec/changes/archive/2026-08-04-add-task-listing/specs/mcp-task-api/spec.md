## ADDED Requirements

### Requirement: List open one-shot tasks through MCP
The MCP server SHALL expose a read-only `list_one_shot_tasks` tool with no required arguments. The tool SHALL execute the Core open-task list query and return structured content containing the established full task representation for each active and paused one-shot task, ordered by ascending durable task ID. The tool description SHALL identify each returned `id` as the identifier used by lifecycle tools.

#### Scenario: List open tasks through MCP
- **WHEN** a client calls `list_one_shot_tasks` after active and paused one-shot tasks have been persisted
- **THEN** the tool returns structured task representations for those tasks in ascending ID order without changing task state or timestamps

#### Scenario: List when no open tasks exist through MCP
- **WHEN** a client calls `list_one_shot_tasks` and no active or paused one-shot tasks exist
- **THEN** the tool returns structured content containing an empty task list
