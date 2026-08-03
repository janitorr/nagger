## Why

The local API mixes an arbitrary snake_case JSON convention with .NET types and duplicates the one-shot task creation input in an HTTP-only request DTO. There are no external consumers, so the API can adopt idiomatic camelCase now while simplifying the endpoint boundary.

## What Changes

- **BREAKING** Standardize every JSON request, response, and validation-error field on ASP.NET Core's default camelCase naming convention.
- **BREAKING** Rename task fields such as `due_at`, `reminder_policy`, `created_at`, `updated_at`, `completed_at`, and `cancelled_at` to camelCase.
- **BREAKING** Rename Morning Report fields such as `schema_version`, `generated_at`, `due_today`, `due_state`, `days_overdue`, and `reminder_policy` to camelCase.
- Bind the one-shot task creation request body directly to `CreateOneShotTaskCommand` and remove the duplicate HTTP request DTO.
- Remove per-property JSON naming attributes that are no longer needed.
- Update API documentation and Host integration tests for the new contract.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `one-shot-task-creation`: Use camelCase task creation fields, task representations, and validation error keys.
- `one-shot-task-lifecycle`: Use camelCase lifecycle timestamp fields in task representations.
- `morning-task-report`: Use camelCase Morning Report response fields.

## Impact

- Affected code: Host API contracts, endpoint binding, JSON serialization attributes, Core creation command validation keys, and Host integration tests.
- Affected documentation: `README.md` and `USAGE.md` request, response, and error examples.
- Affected API: All JSON clients must send and consume camelCase field names.
- Dependencies: No new dependencies.
