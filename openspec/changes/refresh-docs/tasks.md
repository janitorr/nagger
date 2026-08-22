## 1. Refresh README.md

- [ ] 1.1 Move recurring tasks into "Available Now": add a bullet noting recurring templates and instances, and verify the "Coming Next" list no longer contains recurring tasks
- [ ] 1.2 Add the MCP server to "Available Now": a bullet describing the streamable-HTTP endpoint at `/mcp` and its task/report tools, and verify it is mentioned in the README
- [ ] 1.3 Link the Hermes integration guide (`docs/hermes-integration.md`) from the README's top navigation, and verify the link renders and resolves
- [ ] 1.4 Review `git diff README.md` and confirm "Coming Next" still lists only genuinely-planned items (reminder delivery, shopping ledger, deployment automation)

## 2. Refresh docs/product-brief.md

- [ ] 2.1 Update the `updated:` frontmatter date and verify it reflects today's date
- [ ] 2.2 Update the "current release" statement (scope paragraph) to state recurring tasks are shipped, and verify it no longer groups recurrence with planned capabilities
- [ ] 2.3 Remove `*(planned)*` markers from recurring-task use cases and the recurring example heading, and verify none remain (grep for `planned` in the recurring sections)
- [ ] 2.4 Move recurring tasks from "Planned next" into the shipped scope list, and verify the "Planned next" list only contains reminder delivery and deployment automation
- [ ] 2.5 Reconcile the recurring-task model prose (the `lastCompletedAt`/`nextDueAt` description and "Recurring completion" section) with the shipped separate-instance semantics documented in USAGE.md, and verify it no longer contradicts USAGE.md

## 3. Consistency check

- [ ] 3.1 Read README.md and docs/product-brief.md together and verify they agree with USAGE.md on shipped vs planned capabilities (recurring tasks and MCP shipped; reminder delivery, shopping ledger, deployment automation planned)
- [ ] 3.2 Run `git diff` and confirm only the two docs changed and no code, spec, or test files were touched
