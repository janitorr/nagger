## 1. Core Report Behavior

- [x] 1.1 Extend the Core morning-report item contract with `daysUntilDue` and emit schema version `2`.
- [x] 1.2 Restrict active upcoming tasks to the inclusive seven-local-calendar-day visibility window, preserving due-today and overdue item behavior.
- [x] 1.3 Add Core tests for upcoming task detail, the seven-day inclusion boundary, exclusion beyond the window, mutually exclusive timing fields, and read-only behavior.

## 2. REST And MCP Contracts

- [x] 2.1 Map `daysUntilDue` from the Core report through the REST and MCP response contracts.
- [x] 2.2 Update REST integration tests for schema version `2`, upcoming item detail, timing fields, and exclusion outside the visibility window.
- [x] 2.3 Update MCP integration tests for schema version `2` and the upcoming item contract.

## 3. Documentation And Verification

- [x] 3.1 Update API usage and product brief documentation to describe the seven-day report window, `daysUntilDue`, and the retained future role of `reminderPolicy`.
- [x] 3.2 Run `dotnet test Nagger.slnx` and `openspec validate add-seven-day-report-window --strict`.
