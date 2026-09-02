## Why

Two shipped recurring-task behaviors have no corresponding requirements in `openspec/specs/`, so the canonical specs understate what the API and MCP surfaces actually do. Future work that reads the specs would treat those behaviors as unspecified and could diverge from the shipped contract.

## What Changes

- Add requirements to `mcp-task-api` for the three recurring lifecycle tools `pause_recurring_task`, `resume_recurring_task`, and `cancel_recurring_task`, including their template-and-instance cascade and error behavior (#47).
- Add a requirement to `recurring-task-creation` documenting the past-`startDate` rejection, its day granularity, that "today" is accepted, and its configured-timezone semantics (#48).

No code or test changes — the implementation already exists and is tested.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `mcp-task-api`: document the three shipped recurring lifecycle tools that currently have no requirement.
- `recurring-task-creation`: document the past-`startDate` rejection that is implemented but absent from the spec.

## Impact

- `openspec/specs/mcp-task-api/spec.md`
- `openspec/specs/recurring-task-creation/spec.md`
- Possibly `USAGE.md` recurring field table (verify it documents the past-`startDate` rule).
- No code, API, dependency, or schema changes.
