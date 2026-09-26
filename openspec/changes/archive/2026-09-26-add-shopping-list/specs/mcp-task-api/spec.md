# Spec Delta

## MODIFIED Requirements

### Requirement: Read morning reports through MCP
The MCP server SHALL expose a `get_morning_report` tool accepting a `date` in `YYYY-MM-DD` format. It SHALL execute the existing Core morning-report query and return its schema version, generation timestamp, requested date, summary, items, and shopping items.

#### Scenario: Read a morning report through MCP
- **WHEN** a client calls `get_morning_report` with a valid date
- **THEN** the server returns the report classified in the configured timezone, including its open shopping items, without changing task or shopping state

#### Scenario: Reject an invalid report date
- **WHEN** a client calls `get_morning_report` without a date or with a malformed date
- **THEN** the server returns an MCP tool error and does not modify task or shopping state

## ADDED Requirements

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
