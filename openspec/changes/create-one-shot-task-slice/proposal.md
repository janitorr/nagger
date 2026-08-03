## Why

Nagger needs a small, dependable path for capturing a concrete obligation and making it visible to the Hermes Morning Digest without relying on an LLM to infer task state. Establishing this path now validates the local service architecture before recurring tasks, reminder delivery, or shopping expand its scope.

## What Changes

- Add an HTTP command to create an active one-shot task with an explicit due timestamp and reminder policy.
- Add a read-only morning report endpoint at `GET /reports/morning?date=YYYY-MM-DD` that returns deterministic due-state JSON for active one-shot tasks.
- Persist created tasks in a local SQLite database through EF Core migrations.
- Establish the Core/Host boundary: product behavior remains independent of HTTP, SQLite, environment configuration, and the system clock.
- Dispatch Core commands and queries through source-generated Mediator handlers.
- Emit structured JSON logs through source-generated logging methods.

## Capabilities

### New Capabilities
- `one-shot-task-creation`: Create and validate persistable active one-shot tasks through the service API.
- `morning-task-report`: Return versioned, deterministic due-state report data for active one-shot tasks.

### Modified Capabilities

None.

## Impact

- Adds the `Nagger.Core` product module and `Nagger.Host` ASP.NET Core Minimal API host.
- Adds EF Core, the SQLite provider, and a versioned SQLite migration for task persistence.
- Adds the Mediator abstractions and source generator for in-process command/query dispatch.
- Introduces `POST /tasks/one-shot` and `GET /reports/morning` as localhost-only APIs.
- Provides the initial structured JSON contract consumed by Hermes Morning Digest.
- Provides structured JSON operational logs without task titles or other user-entered content.
