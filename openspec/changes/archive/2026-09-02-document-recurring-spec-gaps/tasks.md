## 1. Document recurring MCP lifecycle tools

- [x] 1.1 Sync the three added `mcp-task-api` requirements (pause/resume/cancel recurring tools) into `openspec/specs/mcp-task-api/spec.md` and verify `openspec validate --specs` passes

## 2. Document past-startDate rejection

- [x] 2.1 Sync the modified `recurring-task-creation` requirement (past `startDate` rejection, day granularity, today accepted, IANA-timezone semantics) into `openspec/specs/recurring-task-creation/spec.md` and verify `openspec validate --specs` passes

## 3. Final verification

- [x] 3.1 Confirm `USAGE.md` recurring field table already states the past-`startDate` rule and that no code or test changes are required
