## Purpose

Let a person add items they need to buy so a deterministic shopping list can be built without reconstructing it from free-form notes.

## ADDED Requirements

### Requirement: Add needed shopping items
The service SHALL provide `POST /shopping-items` to add one or more shopping items. Each item SHALL include a nonempty `name` and MAY include a `stores` array of nonempty strings; an omitted `stores` is treated as an empty array. On success the service SHALL persist each item with a stable service-assigned numeric `id`, a `status` of `needed`, and `createdAt`/`updatedAt` timestamps, and SHALL return `201 Created` with a camelCase representation of each item including `id`, `name`, `stores`, `status`, `createdAt`, `updatedAt`, `completedAt`, and `cancelledAt`.

#### Scenario: Add a single item by name
- **WHEN** a client posts a request containing a single item with a nonempty `name` and no `stores`
- **THEN** the service persists a `needed` item with an empty `stores` array and returns `201 Created` with its assigned numeric id

#### Scenario: Add multiple items in one request
- **WHEN** a client posts a request containing several items with nonempty names
- **THEN** the service persists each item as `needed` and returns `201 Created` with one representation per item

#### Scenario: Add an item with stores
- **WHEN** a client posts an item with a nonempty `name` and a nonempty `stores` array
- **THEN** the service persists the item with that `stores` array and returns it in the created representation

#### Scenario: Reject an empty or missing name
- **WHEN** a client posts an item whose `name` is missing or empty
- **THEN** the service returns a structured validation error with camelCase field keys and does not persist that item

### Requirement: Adding an existing name does not duplicate
The service SHALL match item names case-insensitively. Adding a name that already exists SHALL NOT create a duplicate record. Adding a name whose existing record is `bought` or `cancelled` SHALL set that record back to `needed`, preserve its stored `stores`, and update its `updatedAt`.

#### Scenario: Re-adding an already-needed name is a no-op
- **WHEN** a client posts an item whose name already exists with status `needed`
- **THEN** the service does not create a second record and returns the existing item unchanged

#### Scenario: Re-adding a bought item returns it to needed
- **WHEN** a client posts an item whose name already exists with status `bought` and stored `stores`
- **THEN** the service sets that record to `needed`, preserves its `stores`, and updates `updatedAt`

#### Scenario: Name matching is case-insensitive
- **WHEN** a client posts an item whose name differs only in case from an existing name
- **THEN** the service treats it as the same item and does not create a duplicate
