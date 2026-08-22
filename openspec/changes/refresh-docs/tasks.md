## 1. Refresh README.md

- [x] 1.1 Move recurring tasks into "Available Now": add a bullet noting recurring templates and instances, and verify the "Coming Next" list no longer contains recurring tasks
- [x] 1.2 Add the MCP server to "Available Now": a bullet describing the streamable-HTTP endpoint at `/mcp` and its task/report tools, and verify it is mentioned in the README
- [x] 1.3 Link the Hermes integration guide (`docs/hermes-integration.md`) from the README's top navigation, and verify the link renders and resolves
- [x] 1.4 Review `git diff README.md` and confirm "Coming Next" still lists only genuinely-planned items (reminder delivery, shopping ledger, deployment automation)

## 2. Refresh docs/product-brief.md

- [x] 2.1 Update the `updated:` frontmatter date and verify it reflects today's date
- [x] 2.2 Update the "current release" statement (scope paragraph) to state recurring tasks are shipped, and verify it no longer groups recurrence with planned capabilities
- [x] 2.3 Remove `*(planned)*` markers from recurring-task use cases and the recurring example heading, and verify none remain (grep for `planned` in the recurring sections)
- [x] 2.4 Move recurring tasks from "Planned next" into the shipped scope list, and verify the "Planned next" list only contains reminder delivery and deployment automation
- [x] 2.5 Reconcile the recurring-task model prose (the `lastCompletedAt`/`nextDueAt` description and "Recurring completion" section) with the shipped separate-instance semantics documented in USAGE.md, and verify it no longer contradicts USAGE.md

## 3. Consistency check

- [x] 3.1 Read README.md and docs/product-brief.md together and verify they agree with USAGE.md on shipped vs planned capabilities (recurring tasks and MCP shipped; reminder delivery, shopping ledger, deployment automation planned)
- [x] 3.2 Run `git diff` and confirm only the two docs changed and no code, spec, or test files were touched

## 4. Add captured assistant conversation (issue #29)

- [x] 4.1 Add a conversation block under "What Nagger Does", after the existing paragraph and before "Available Now", showing the dry-cleaning task end to end (create → due-today rundown → complete) plus the "Review the quarterly metrics deck" upcoming state, and verify both report states appear
- [x] 4.2 Use the exact MCP tool names `create_one_shot_task` and `complete_one_shot_task` with the real payload fields (due `2026-08-25T17:00:00+03:00`, policy `once`), and verify they match the MCP contracts in USAGE.md
- [x] 4.3 Keep tool calls as short notes (not raw JSON) and verify USAGE.md is untouched
