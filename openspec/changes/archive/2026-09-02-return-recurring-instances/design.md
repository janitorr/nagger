## Context

Both recurring write paths already create the instance row they need to report:

- `CreateRecurringTaskHandler` persists the first instance and calls `instanceStore.AddAsync(firstInstance, ...)`, which returns the persisted instance (with its assigned id), but the handler returns only the template.
- `CompleteRecurringTaskHandler` persists the next instance the same way and returns only the completed instance.

The stores return the persisted model with its id already assigned (SQLite via EF's `SaveChangesAsync`; the in-memory test double assigns `Id = Count + 1`), so surfacing the instance costs nothing extra. The change is purely in the return types and the two response surfaces (REST + MCP), which both map a single Core result 1:1 today. See proposal.md for motivation.

## Goals / Non-Goals

**Goals:**

- Return the first instance with `create` and the next instance with `complete`, through both REST and MCP.
- Keep Core transport-agnostic: Core returns a result record, Host adapts it to HTTP and MCP envelopes.
- Reuse the existing full template/instance representations rather than inventing a trimmed "summary" shape.

**Non-Goals:**

- No storage, EF mapping, or migration changes.
- No change to `pause`/`resume`/`cancel`/`list` recurring responses.
- No change to the `Location` header on create (still `/tasks/recurring/{template.Id}`).
- Not documenting the already-undocumented MCP recurring tools (`pause`/`resume`/`cancel`) beyond the two this change touches.

## Decisions

**1. Envelope response shape (breaking).** Both responses become objects with two named keys rather than a flat representation:

- create → `{ "template": <template>, "firstInstance": <instance> }`
- complete → `{ "completedInstance": <instance>, "nextInstance": <instance> }`

Rationale: symmetric and explicit for the sole caller (an LLM agent), and it keeps the top-level keys self-describing. Alternatives considered: an additive field (`firstInstance`/`nextInstance` nested inside the existing flat object) was rejected for being less symmetric; a trimmed `{ id, dueAt }` summary was rejected because the full instance representation already exists and is free to reuse.

**2. Core result records instead of a new domain entity.** Add `CreateRecurringTaskResult(Template, FirstInstance)` and `CompleteRecurringTaskResult(CompletedInstance, NextInstance)` records beside their commands, changing the commands' `ICommand<T>` parameter. Rationale: the instance is a persisted domain entity; pairing it with a sibling result is an orchestration concern, not a property of the entity. The handlers capture the `AddAsync` return value instead of discarding it.

**3. Host adapts via new response records.** Add `RecurringCreationResponse` / `RecurringCompletionResponse` in `RecurringTaskEndpoints.cs`, and `McpRecurringCreationResponse` / `McpRecurringCompletionResponse` in `McpTaskTools.cs`, each with `From(...)` mappers that reuse the existing `RecurringTemplateResponse`/`RecurringTaskInstanceResponse` (and their MCP counterparts) for the inner objects. Rationale: matches the existing per-surface record convention; keeps REST and MCP mapping logic local to each adapter.

**4. MCP tool descriptions are updated as part of the contract.** The `create_recurring_task` and `complete_recurring_task` `[Description]` strings are rewritten to state that the response contains both objects and to point at `firstInstance.dueAt` / `nextInstance.dueAt`. Rationale: the only consumer is an LLM agent that navigates by description text, so the description is as much the contract as the schema.

## Risks / Trade-offs

- [Breaking change to REST and MCP response shapes] → Acceptable; the only caller is the internal LLM agent, and the issues explicitly request the new shape.
- [The `create` envelope ripples through test helpers] → `CreateRecurringTemplateAsync` (ApiTests) and `CreateRecurringTaskAsync` (McpTests) read the template id from the flat top level; both become one-line `GetProperty("template").GetProperty("id")` edits, and every recurring lifecycle test using them is affected in the diff.
- [MCP `OutputSchemaType` must stay in sync with the serialized content] → Both derive from the same response record, so `structuredContent` and `Text` serialize identically; tests assert on `structuredContent`.
