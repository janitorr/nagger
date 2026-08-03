## 1. Solution and Core Foundation

- [ ] 1.1 Create the .NET solution with `Nagger.Core`, `Nagger.Host`, and focused Core and Host test projects; add `Mediator.Abstractions` to Core and reference Core only from Host and test projects as appropriate.
- [ ] 1.2 Define the shared one-shot task model, supported reminder-policy values, task identity, timestamps, and schedule fields in Core.
- [ ] 1.3 Define minimal Core ports for task persistence and supplied current time/configured timezone without infrastructure references.
- [ ] 1.4 Implement the Mediator create-one-shot-task command and handler with local Core validation for required title, offset-qualified due timestamp, and explicit supported reminder policy.
- [ ] 1.5 Add meaningful Core unit tests for successful creation and every specified creation validation failure without HTTP or SQLite dependencies.

## 2. Deterministic Morning Reporting

- [ ] 2.1 Implement the Mediator Core morning-report query and handler, including configured-timezone date comparison, due-today/overdue/upcoming summary counts, and overdue-day calculation.
- [ ] 2.2 Limit detailed report items to active due-today and overdue one-shot tasks, and include the specified version and generation metadata in the report result.
- [ ] 2.3 Ensure the report query uses read-only persistence behavior and does not alter task or reminder fields.
- [ ] 2.4 Add meaningful Core unit tests for each due-state classification, overdue-day calculation, report-date validation, repeated reads, and timezone boundary behavior without Host wiring or database dependencies.

## 3. SQLite Host Adapter

- [ ] 3.1 Add ASP.NET Core Minimal API, EF Core SQLite, migration tooling, `Mediator.Abstractions`, and `Mediator.SourceGenerator` dependencies to the Host project; configure generated mediator registration for the Core assembly.
- [ ] 3.2 Implement the Host SQLite `DbContext`, entity mapping, Core persistence-port adapter, configured database path, and current-time/timezone adapter.
- [ ] 3.3 Create and verify the initial EF Core migration for one-shot task persistence.
- [ ] 3.4 Add Host integration tests that apply the migration to SQLite and verify create and report operations persist and retrieve task data.

## 4. HTTP API and Verification

- [ ] 4.1 Map `POST /tasks/one-shot` to the Core command, return `201 Created` for success, and return structured JSON validation errors without persistence for invalid input.
- [ ] 4.2 Map `GET /reports/morning?date=YYYY-MM-DD` to the Core query, return structured JSON validation errors for missing or malformed dates, and configure the host to listen on localhost only.
- [ ] 4.3 Configure JSON console logging and source-generated `LoggerMessage` methods with stable event ids for request completion, validation rejection, task creation, and unexpected failures; exclude task titles and request bodies from application logs.
- [ ] 4.4 Add Host integration tests against the assembled HTTP application and SQLite covering Mediator dispatch, successful creation, invalid creation input, due-today and overdue report output, upcoming summary behavior, and non-mutating repeated report reads.
- [ ] 4.5 Add Host integration tests that verify validation errors and unhandled application failures are translated by the configured API exception handlers without leaking implementation details.
- [ ] 4.6 Add logging tests or assertions that verify structured operational fields and prevent user-entered task content from being emitted.
- [ ] 4.7 Run the complete test suite and verify the initial migration and report output against a local SQLite database using SQLite tooling.
