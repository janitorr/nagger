## Context

The Host currently exposes snake_case JSON names through per-property serialization attributes and maps `CreateTaskRequest` to an otherwise equivalent Core command. The service has no external users, so it can replace this inconsistent API convention without a compatibility period.

## Goals / Non-Goals

**Goals:**
- Make all JSON field names camelCase through ASP.NET Core's default HTTP JSON behavior.
- Bind the create-task request directly to the Core command without adding HTTP serialization concerns to Core.
- Preserve existing task, lifecycle, report, validation, persistence, and status behavior.

**Non-Goals:**
- Add a compatibility layer for snake_case clients.
- Change endpoint paths, response status codes, timestamp formats, task state transitions, or report semantics.
- Introduce a JSON serializer dependency or non-default naming policy.

## Decisions

### Use the default camelCase HTTP JSON contract

Remove the explicit JSON property-name attributes and rely on ASP.NET Core's default `System.Text.Json` camelCase policy. This applies consistently to every JSON request and response, including task and report contracts, without adding serialization attributes to Core.

An application-wide snake_case policy was considered, but was rejected because snake_case has no domain requirement and would preserve the non-idiomatic .NET convention.

### Bind creation requests directly to the Core command

The creation endpoint will accept `CreateOneShotTaskCommand` directly and pass it to the mediator. This removes `CreateTaskRequest` and its one-to-one mapping while retaining the command's existing validation behavior.

A Host DTO was considered for strict adapter isolation, but it provides no meaningful translation or validation beyond the command's identical fields.

### Retain string command values and Core validation

`DueAt` and `ReminderPolicy` remain nullable strings in the command. The handler will continue parsing and validating them so malformed payloads still receive the established structured validation response. Validation keys change only to camelCase.

Parsing at HTTP model binding was considered, but it would change validation ownership and error behavior without simplifying the API contract.

## Risks / Trade-offs

- [All JSON fields are breaking changes] -> Update all checked-in API documentation, OpenSpec requirements, and Host integration tests in the same change; no external migration is required.
- [Direct binding exposes a Core command as an HTTP body] -> The command remains free of HTTP attributes and has the same input shape as the local API; introduce a DTO later only if another adapter needs a different representation.
- [Default serializer behavior could be obscured] -> Do not configure a custom naming policy; test representative create, lifecycle, and report JSON fields.
