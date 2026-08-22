## Why

The Host logs request/status/duration with a hand-rolled `Stopwatch` middleware that only observes the REST surface: every MCP tool call appears as an opaque `POST /mcp 200`, so no tool-level diagnostics exist. Both surfaces already dispatch through `Mediator`, so a pipeline behavior is the single chokepoint that can log command/query dispatch uniformly across REST and MCP. At the same time, validation failures return a bespoke `ValidationError` record where the platform's `ValidationProblemDetails` envelope would do.

## What Changes

- Add a `DispatchLoggingBehavior<TMessage, TResponse>` implementing `IPipelineBehavior<,>` that times each dispatch and logs the message type, elapsed milliseconds, and (on failure) the error type, then rethrows so transport adapters still shape the response.
- Remove the hand-rolled `Stopwatch` request-logging middleware from `Program.cs`.
- Fold failure logging into the behavior: `ValidationException` at Warning, not-found (`TaskNotFoundException`/`RecurringTaskNotFoundException`) at Debug, other exceptions at Error. Remove `AppLog.RequestCompleted`, `AppLog.ValidationRejected`, and `AppLog.UnexpectedFailure`; add dispatch log messages.
- Make `ApiExceptionHandler` shape responses only (no logging).
- Return `Results.ValidationProblem(errors)` (a `ValidationProblemDetails` response) for `ValidationException`, replacing the custom `ValidationError` record; delete `src/Nagger.Host/Api/ValidationError.cs`.
- Update the AGENTS.md "Host Organization" logging rule to reflect that Mediator pipeline behavior owns dispatch diagnostics, replacing hand-rolled HTTP middleware.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. No spec-level requirement changes: validation still returns `400` with an `errors` object keyed by camelCase field names, and logging is not part of any spec. This opts out of specs via `skip_specs: true`.

## Impact

- `src/Nagger.Host/Composition/Mediator/` — new `DispatchLoggingBehavior` and its registration in `AddNaggerMediator`.
- `src/Nagger.Host/Program.cs` — remove the custom middleware.
- `src/Nagger.Host/AppLog.cs` — replace the three request/failure log methods with dispatch log methods; keep `TaskCreated`.
- `src/Nagger.Host/Api/ExceptionHandling/ApiExceptionHandler.cs` — validation response shape; remove logging.
- `src/Nagger.Host/Api/ValidationError.cs` — deleted.
- `tests/Nagger.Host.Tests/ApiTests.cs` — remove the `RequestCompleted`, `ValidationRejected`, and `UnexpectedFailure` log tests; add log tests for the new `DispatchSucceeded`/`DispatchValidationFailed`/`DispatchNotFound`/`DispatchFailed` messages mirroring the existing "does not include task content" pattern; validation-error assertions remain valid (they read the `errors` property).
- `AGENTS.md` — update the Host Organization logging rule.
- No Core, EF, migration, or MCP changes.
