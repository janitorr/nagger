# Proposal

## Why

`list_recurring_tasks` and `GET /tasks/recurring` return bare recurring templates (id, title, startDate, recurrence, status) with no indication of when the current instance is due. The create and complete paths already surface instance data (`firstInstance`, `completedInstance`, `nextInstance`), but the list — the discovery surface an agent uses to pick a template for pause/resume/cancel — is still blind to "what's next". An agent listing tasks cannot tell which recurring obligation is actually coming up without a separate report call or a direct DB read (issue #73).

## What Changes

- Add a nullable `nextDueAt` field (`DateTimeOffset`, ISO-8601 with an explicit offset) to each template in the list response on both surfaces: the MCP `list_recurring_tasks` tool and the REST `GET /tasks/recurring` endpoint.
- `nextDueAt` is the template's current open instance's `DueAt` — the open instance being the single `active` or `paused` instance at rest. It is `null` when the template has no open instance (cancelled).
- The list stays read-only and continues to return templates ordered by ascending template id. The returned `id` remains the template id consumed by the recurring lifecycle tools; no instance id is surfaced.

This is additive: existing callers that ignore the new field and act by template id are unchanged.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `mcp-task-api`: the "List recurring task templates through MCP" requirement gains a `nextDueAt` field on each returned template.
- `recurring-task-listing`: the "List recurring task templates" requirement gains a `nextDueAt` field on each returned template.

## Impact

- Core: `ListRecurringTemplatesQuery` result now carries the open instance's due date; the recurring-template store hydrates open instances on the list read path.
- Host: `McpRecurringTemplateResponse` and `RecurringTemplateResponse` each gain the `nextDueAt` field; `SqliteRecurringTaskTemplateStore.GetAllAsync` hydrates open instances.
- Documentation: `USAGE.md` list sections updated to describe `nextDueAt`.
- Tests: Core and Host tests cover the new field, including the cancelled → `null` case.
