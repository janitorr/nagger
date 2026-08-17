## Why

Nagger currently only supports one-shot tasks. Users need recurring tasks to manage repeated obligations (weekly meetings, monthly bills, etc.) that keep respawning. This is a core planned capability from the product brief.

## What Changes

- Add recurring task templates that generate one-shot task instances
- Add endpoints for creating and managing recurring task templates
- Modify task completion to generate next instance for recurring tasks
- Add pause/resume/cancel operations for recurring task templates

## Capabilities

### New Capabilities
- `recurring-task-creation`: Create recurring task templates that generate first instance
- `recurring-task-lifecycle`: Complete, pause, resume, cancel recurring task templates
- `recurring-task-listing`: List recurring task templates

### Modified Capabilities
- `one-shot-task-lifecycle`: Completing a recurring-generated instance creates next instance

## Impact

- New database table for recurring task templates
- New REST endpoints under `/tasks/recurring/`
- New MCP tools for recurring task operations
- Modifications to one-shot task completion logic to handle recurring instances
