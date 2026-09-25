# Spec Delta

## MODIFIED Requirements

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
