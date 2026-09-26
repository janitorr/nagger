# Spec Delta

## Purpose

Record a dateless list of household items the user needs to buy and manage that list through the local service API.

## ADDED Requirements

### Requirement: Add a shopping item
The service SHALL provide `POST /shopping` to add a shopping item. The request SHALL include a `name` that is nonempty after surrounding whitespace is trimmed and is at most 200 characters.

Adding is idempotent by name: names SHALL be matched case-insensitively and ignoring surrounding whitespace, so adding a name that is already on the list SHALL NOT create a duplicate. When the name is new, the service SHALL persist an item with a stable service-assigned numeric `id` and return `201 Created` with the item representation containing `id` and `name`. When the name is already on the list, the service SHALL return `200 OK` with the existing item representation.

#### Scenario: Add a shopping item
- **WHEN** a client posts a nonempty `name` that is not on the list
- **THEN** the service persists a shopping item and returns `201 Created` with its assigned `id` and `name`

#### Scenario: Add is idempotent by name
- **WHEN** a client posts a `name` that is already on the list, differing only in letter case or surrounding whitespace
- **THEN** the service returns `200 OK` with the existing item and does not create a duplicate

#### Scenario: Reject an empty name
- **WHEN** a client posts a missing or whitespace-only `name`
- **THEN** the service returns a structured JSON validation error and does not persist an item

#### Scenario: Reject an over-long name
- **WHEN** a client posts a `name` longer than 200 characters
- **THEN** the service returns a structured JSON validation error and does not persist an item

### Requirement: Remove a shopping item
The service SHALL provide `DELETE /shopping/{name}` to remove a shopping item. It SHALL remove the open item whose `name` matches case-insensitively and ignoring surrounding whitespace.

Removing is idempotent: when no item matches the name, the service SHALL return a successful response and leave the list unchanged.

#### Scenario: Remove a shopping item
- **WHEN** a client deletes a `name` that is on the list
- **THEN** the service removes the matching item so that it no longer appears when the list is read

#### Scenario: Remove an absent name
- **WHEN** a client deletes a `name` that is not on the list
- **THEN** the service returns a successful response and leaves the list unchanged

### Requirement: List shopping items
The service SHALL provide `GET /shopping` to list open shopping items. It SHALL return `200 OK` with an array of item representations, each containing `id` and `name`, ordered by ascending `id`. When no items exist, the response SHALL be an empty array.

#### Scenario: List shopping items in insertion order
- **WHEN** shopping items have been persisted
- **THEN** the service returns them ordered by ascending `id`

#### Scenario: List when no items exist
- **WHEN** no shopping items exist
- **THEN** the service returns an empty array
