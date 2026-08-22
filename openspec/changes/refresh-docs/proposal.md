## Why

`README.md` and `docs/product-brief.md` describe the project as if recurring tasks are still planned, but they have been shipped (REST endpoints, MCP tools, and `openspec/specs/` coverage since 2026-08-17). The README also omits the MCP server entirely even though it is Nagger's primary assistant-facing surface. The docs now contradict the code and each other.

## What Changes

- `README.md`:
  - Move recurring tasks from "Coming Next" to "Available Now".
  - Add the MCP server to "Available Now" (13 tools over streamable HTTP).
  - Link the Hermes integration guide (`docs/hermes-integration.md`), which USAGE.md already references.
  - Keep genuinely-planned items ("reminder delivery", "shopping ledger", "deployment automation") under "Coming Next".
- `docs/product-brief.md`:
  - Remove the `*(planned)*` markers from recurring-task use cases and examples.
  - Move recurring tasks from "Planned next" into the shipped scope.
  - Update the `updated:` frontmatter date.

No code, API, or runtime behavior changes. USAGE.md, AGENTS.md, and DEVELOPMENT.md were reviewed and are current; they are out of scope.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This is a documentation-only change with no spec-level behavior changes, so the change opts out of spec deltas via `skip_specs: true`.

## Impact

- Affected files: `README.md`, `docs/product-brief.md`.
- No code, API, dependency, or database changes.
- No tests affected (no test documents the README roadmap or product-brief text).
