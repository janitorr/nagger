## Context

`Program.cs` registers a hand-rolled middleware (lines 32-44) that wraps `next`, starts a `Stopwatch`, and emits `AppLog.RequestCompleted(path, statusCode, elapsedMs)`. Every command/query flows through `Mediator` (martinothamar's `Mediator`, v3.0.2), from both the REST endpoints and the MCP tools. The Mediator library supports `IPipelineBehavior<TMessage, TResponse>` with a `Handle(TMessage, CancellationToken, MessageHandlerDelegate<TMessage, TResponse> next)` method, registered manually as an open generic. See proposal.md - Why for motivation.

## Goals / Non-Goals

**Goals:**

- One dispatch-logging point that observes both REST and MCP command/query dispatches, logging message type, elapsed time, and failure type.
- Preserve the failure signal the current `ValidationRejected` (Warning) and `UnexpectedFailure` (Error) logs provide, without HTTP-only coupling.
- Use `ValidationProblemDetails` for validation error responses, preserving the `errors` object keyed by camelCase field names.

**Non-Goals:**

- Not logging HTTP transport details (path, status code) — the behavior logs the domain operation, not the transport.
- Not introducing `AddValidation()` or any DataAnnotations-based endpoint validation.
- Not touching Core, EF mappings/migrations, or the MCP tool surface.

## Decisions

### Decision: `DispatchLoggingBehavior<,>` as the sole dispatch-logging point

Add a `DispatchLoggingBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>` that wraps `next`, times it with a `Stopwatch`, and logs outcome, then rethrows on failure. Register it in `AddNaggerMediator` via `services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(DispatchLoggingBehavior<,>))`.

- **Why:** The behavior is the only place that sees both transports' dispatches. It logs `{MessageType} {ElapsedMs}ms` on success and `{MessageType} {ErrorType} {ElapsedMs}ms` on failure, which is strictly more useful than `POST /mcp 200 in 3ms`.
- **Alternative considered:** Keep HTTP logging and add the behavior alongside. Rejected: two overlapping logging layers are noise for a single-user local service, and HTTP-only logging cannot name which MCP tool ran.

### Decision: Classify failures by exception type, log then rethrow

The behavior catches exceptions, logs `ValidationException` at Warning, `TaskNotFoundException`/`RecurringTaskNotFoundException` at Debug, and everything else at Error, and always rethrows. The transport adapters (`ApiExceptionHandler`, MCP `Run` helper) keep their exception-to-response shaping.

- **Why:** The current failure signals live in `ApiExceptionHandler` (HTTP-only), so MCP failures are silently unlogged. Moving classification into the behavior gives both surfaces failure logging while keeping response shaping where it belongs. This mirrors the library's documented "error logging" pipeline example. Not-found is a third category: a normal, recoverable client outcome mapped to 404, so it logs at Debug rather than Error to avoid noise and keep genuine failures distinguishable.
- **Alternative considered:** Log only success+timing, keep `ValidationRejected`/`UnexpectedFailure` in the handler. Rejected: that leaves MCP failures unlogged and splits the concern across two files.

### Decision: Behavior lives in Host, logs via `AppLog` source-generated messages

The behavior sits in `src/Nagger.Host/Composition/Mediator/` and uses `AppLog` `LoggerMessage` source-generated methods, since it needs `ILogger` and Core must stay free of runtime/logging dependencies.

- **Why:** Core has no logging dependency; the behavior is host infrastructure. Reusing `AppLog` keeps the existing structured-logging style and event-id convention.

### Decision: `ApiExceptionHandler` shapes, does not log

Remove `AppLog.ValidationRejected` and `AppLog.UnexpectedFailure` from `ApiExceptionHandler`; it becomes pure response-shaping (`400` + `ValidationProblemDetails`, `404`, `500` ProblemDetails).

### Decision: `Results.ValidationProblem` for validation errors

Replace `WriteAsJsonAsync(new ValidationError(...))` with `Results.ValidationProblem(validation.Errors.ToDictionary(e => e.Key, e => e.Value)).ExecuteAsync(context)`.

- **Why:** `ValidationProblemDetails` keeps the `errors` object with camelCase keys, so host tests reading `body.RootElement.GetProperty("errors")` stay valid. `ValidationException.Errors` is `IReadOnlyDictionary<string, string[]>`, so `.ToDictionary(...)` bridges to the `IDictionary<string, string[]>` overload. `ValidationProblem` sets `400` and `Content-Type`, making the explicit status assignment redundant.
- **Alternative considered:** `WriteAsJsonAsync(new ValidationProblemDetails(errors))`. Rejected: `Results.ValidationProblem(...)` is the typed-result idiom already used in this host (`Results.Problem` for the 500 path).

## Risks / Trade-offs

- [Loss of HTTP path/status in logs] → Acceptable: the domain operation name is the more useful signal for this app, and error status is implied by the exception type.
- [`Results.ValidationProblem` adds `title`/`status` fields not present in `ValidationError`] → Additive; specs require only the `errors` object, and tests only read `errors`.
- [Behavior logging runs on every dispatch, including successful reads] → Acceptable; matches the current per-request logging volume, now at the operation level.

## Migration Plan

No persistence, schema, or external-contract migration. Deploy by rebuilding and restarting the host; rollback is a revert of the affected host files.
