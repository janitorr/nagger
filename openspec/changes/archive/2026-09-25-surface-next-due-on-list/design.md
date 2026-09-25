# Design

## Context

Since the `template-owns-instances` change, `RecurringTaskTemplate` is the aggregate root owning its open instances (`Instances`, hydrated as `active` or `paused`). `GetByIdAsync` hydrates those open instances via a filtered `.Include`; `GetAllAsync` deliberately loads templates only (its decision #7). The list query `ListRecurringTemplatesQuery` returns `IReadOnlyList<RecurringTaskTemplate>`, consumed identically by the REST `GET /tasks/recurring` endpoint and the MCP `list_recurring_tasks` tool, whose response records (`RecurringTemplateResponse` / `McpRecurringTemplateResponse`) are currently field-identical. See proposal.md - Why for motivation.

At rest, the state machine guarantees at most one open instance per template: `active` templates hold one `active` instance, `paused` templates hold one `paused` instance, and `cancelled` templates hold none. So "next due" is exactly the open instance's `DueAt`; there is no separately-stored future date.

## Goals / Non-Goals

**Goals:**

- Surface a single nullable `nextDueAt` field on both list surfaces.
- Keep the change additive and derive the value from the existing open-instance hydration.

**Non-Goals:**

- No instance id is surfaced; the template id remains the only handle lifecycle tools consume.
- No nested instance object or `instances[]` array in the list.
- No change to lifecycle tools, the morning report, or instance retention.
- No schema or data migration.

## Decisions

**1. A single scalar `nextDueAt` (`DateTimeOffset?`), not a nested instance.**
No lifecycle tool accepts an instance id — every one takes the template id — and the template's `status` already distinguishes active/paused/cancelled. The instance's other fields would be dead weight on this surface.
*Alternative (rejected)*: nested `currentInstance` reusing `McpRecurringInstanceResponse`/`RecurringTaskInstanceResponse` — consistent with create/complete but carries `recurringTaskId`, `title`, `type`, and timestamps the caller cannot act on.
*Alternative (rejected)*: a full `instances[]` array — re-opens the history-query concern the prior change's decision #7 closed.

**2. `nextDueAt` reports the open instance's due date for both active and paused templates; `null` only when there is no open instance.**
This gives the field one stable meaning — "the date this would next fire" — so a paused template still shows when it will pop if resumed. An agent can flag an overdue paused template and suggest cancelling it. `null` then means "nothing currently scheduled," which happens only for cancelled templates.
*Alternative (rejected)*: `null` for paused — collapses paused and cancelled onto the same "when" value, discarding information the `status` field cannot recover on its own.

**3. Hydrate open instances in `GetAllAsync`, mirroring `GetByIdAsync`.**
`GetAllAsync` gains the same filtered `.Include(x => x.Instances.Where(active || paused))` used by `GetByIdAsync`. This is one extra relationship load on a local SQLite store.
*Alternative (rejected)*: a bespoke scalar projection (e.g. `MAX(DueAt)` over open instances) — more query machinery for no benefit at this scale.

**4. Expose the open instance as a domain concept, then project at the boundary.**
Add a `CurrentInstance` computed property on `RecurringTaskTemplate` returning the single open (`active` or `paused`) instance (or `null`). Both response records map `template.CurrentInstance?.DueAt` to `nextDueAt`, keeping the two projection sites one-liners and giving Core a directly testable unit.
*Alternative (rejected)*: duplicating the "pick the open instance" predicate inside each Host record — small, but splits a domain concept across two adapters.

**5. Add the field to both response records together.**
`RecurringTemplateResponse` and `McpRecurringTemplateResponse` stay field-identical, preserving the REST/MCP parity the codebase already maintains.

## Risks / Trade-offs

- [No DB-level "one open instance" constraint] → `CurrentInstance` picks the first match if data ever held more than one open instance. The state machine guarantees one, and `Complete` already makes the same assumption; a partial unique index is a future option.
- [List now performs a per-template join] → negligible on local SQLite; revisit only if the list path ever shows as a real query problem.
- [Additive field vs. strict schema clients] → new field is additive; clients that ignore unknown fields are unaffected. The MCP tool's advertised output schema (`McpRecurringTemplateListResponse` → `McpRecurringTemplateResponse`) is updated so it matches the returned shape.

## Migration Plan

No schema or data migration. The change is an EF query update, one Core computed property, and two response-record field additions. Rollback = revert those files.

## Open Questions

None.
