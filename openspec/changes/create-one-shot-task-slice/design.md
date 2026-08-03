## Context

Nagger is an empty repository with a product brief and a product design. The first executable path must prove that a local service can accept an explicit one-shot obligation, persist it, and produce machine-readable due-state data for Hermes Morning Digest. The service runs on ARM64 Linux, listens only on localhost, and uses the configured `Europe/Helsinki` IANA timezone to interpret report dates.

## Goals / Non-Goals

**Goals:**
- Establish `Nagger.Core` as a product module that contains task validation and due-state behavior without references to ASP.NET Core, EF Core, SQLite, configuration, or the system clock.
- Establish `Nagger.Host` as an ASP.NET Core Minimal API adapter and composition root that supplies HTTP mapping, SQLite persistence, configuration, and time.
- Prove a versioned EF Core migration and a local SQLite database through one persisted one-shot task path.
- Define stable JSON contracts for task creation and a pure morning-report read.

**Non-Goals:**
- Recurring tasks, task edits, pause/complete/cancel transitions, or reminder emission.
- A history or event table, delivery idempotency, authentication, remote access, or systemd deployment assets.
- Shopping behavior and report output.
- A policy for including future tasks as detailed morning-report items.

## Decisions

### Core/Host module boundary

Create two projects with a one-way dependency from `Nagger.Host` to `Nagger.Core`. Core declares small persistence and time ports required by its feature handlers; Host implements them with EF Core/SQLite and a configured clock/timezone.

This prevents HTTP and persistence behavior from leaking into task rules, while avoiding additional service or repository abstractions. A single ASP.NET project would be simpler initially, but would couple deterministic behavior to infrastructure and weaken the design's testability goal.

### Vertical feature files in Core

Organize the initial task behavior by feature, with command/query request types, result types, handlers, and local validation co-located by default. Extract shared task model types only where creation and reporting require the same invariant or representation.

This follows the product design's navigation convention and avoids prematurely creating layers for a two-endpoint service.

### Source-generated in-process mediation

Use the `Mediator` NuGet packages for Core feature dispatch. `Nagger.Core` references `Mediator.Abstractions` and defines each feature as a command or query with a single handler. `Nagger.Host` references both `Mediator.Abstractions` and `Mediator.SourceGenerator`, configures `AddMediator` with the Core assembly and scoped lifetime, and maps HTTP endpoints to mediator sends.

The generator package is installed only in Host, the outermost executable, as required by Mediator. This preserves Core's infrastructure independence while making endpoint-to-feature dispatch explicit and providing build-time diagnostics for missing or duplicate handlers. Direct endpoint-to-handler service injection is a viable smaller alternative, but it would make the chosen command/query feature convention optional and would not establish the mediation boundary needed by later slices.

Mediator pipeline behaviors are not introduced in this slice. Feature validation remains local to Core handlers; request logging is owned by the HTTP host rather than a generic message pipeline.

### SQLite as the canonical store

Host owns an EF Core `DbContext`, SQLite mapping, database-path configuration, and versioned migrations. The initial schema stores the fields necessary for an active one-shot task: stable numeric id, title, type, status, creation/update timestamps, due timestamp, reminder policy, and reminder timestamps.

SQLite is embedded, locally inspectable, and needs no network dependency. A file-based custom store would reduce packages but would not establish the required migration path or provide the same schema evolution safety.

### Explicit time at the boundary

Host supplies the current timestamp and configured IANA timezone to Core. Persisted timestamps are ISO-8601 instants with explicit UTC offsets. Core compares the local calendar date of a task's due timestamp with the requested report date; it does not treat time of day as a due-state boundary.

Using an injected clock makes creation timestamps and report generation deterministic in tests. Calling the system clock in handlers would make correctness around midnight and daylight-saving transitions difficult to test.

### Narrow HTTP contract

Expose `POST /tasks/one-shot` and `GET /reports/morning?date=YYYY-MM-DD` on localhost. Creation requires a nonempty title, a due timestamp with explicit offset, and an explicit reminder policy. Invalid input returns a structured JSON validation error; successful creation returns `201 Created` with the assigned task representation.

The report returns `schema_version`, `generated_at`, the requested date, summary counts for active tasks in each due state, and item detail for active tasks that are due today or overdue. Report reads make no writes. Detailed presentation of upcoming tasks is deferred, but their count is retained in the summary.

Separating commands from report reads preserves the product's guarantee that a digest retry cannot mutate reminder or task state. Returning all upcoming task detail now would create an unvalidated Morning Digest policy.

### Structured source-generated logging

Host configures JSON console logging so every emitted application log is machine-readable. Application log statements use `LoggerMessage` source-generated partial methods with stable event ids, levels, and named fields; direct `ILogger.Log*` calls are not used for application events.

Log request completion, validation rejection, successful task creation, and unexpected failures with operational identifiers such as request path, HTTP status, task id, due state, and elapsed duration. Do not log task titles, raw request bodies, or other user-entered content because reminder data can be personal. Framework logging remains governed by standard ASP.NET Core category configuration.

JSON console logging is preferred to plaintext templates because the service is intended for `systemd` operation and local diagnostic tooling. Source-generated methods avoid template parsing and make event metadata reviewable at compile time.

### Testing boundary

Core is verified with meaningful unit tests that exercise task validation, task creation, due-state classification, date and timezone boundaries, and report purity without HTTP, EF Core, or the system clock. Unit tests do not duplicate framework binding or database wiring behavior.

Host is verified through integration tests against the assembled application and a SQLite database. These tests exercise HTTP request binding, Mediator dispatch, persistence, response serialization, validation-error mapping, and the API exception-handling path. The integration suite is the authority that the composition root is correctly wired; it does not repeat every Core business-rule permutation.

## Risks / Trade-offs

- [Date-time conversion at daylight-saving boundaries is incorrect] -> Centralize conversion through the configured IANA timezone and cover date classification with boundary-focused Core tests.
- [EF Core migrations are difficult to run on the Pi] -> Verify migration creation and application against SQLite in automated integration tests; keep migration ownership in Host.
- [The initial API contract expands before a real consumer uses it] -> Limit creation to required one-shot fields and leave recurring, editing, and delivery endpoints out of this change.
- [SQLite file location or permissions prevent startup] -> Make the database path explicit Host configuration and fail startup with a clear error if it cannot be opened.
- [Logs reveal private reminder content] -> Limit structured fields to operational metadata and prohibit task titles and request bodies from application logs.
- [Mediator handlers are registered with an inappropriate lifetime] -> Use Mediator's scoped lifetime to match the scoped EF Core SQLite adapter and verify endpoint dispatch in Host integration tests.
- [Core and Host tests duplicate each other or leave wiring untested] -> Keep domain-rule coverage in Core unit tests and reserve assembled HTTP, persistence, and exception-handler coverage for Host integration tests.

## Migration Plan

1. Add the Core and Host projects and the initial SQLite migration.
2. Configure a local development database path and apply the migration when the service starts or through the standard EF migration command selected during implementation.
3. Deploy the Host with a configured writable local database path; no existing production data requires migration for this first release.
4. Roll back by stopping the new service and retaining the SQLite database file for inspection. Schema rollback is unnecessary until a subsequent migration changes deployed data.

## Open Questions

- The exact production database path and whether migrations run automatically at startup remain deployment decisions; neither changes the HTTP or task contracts.
