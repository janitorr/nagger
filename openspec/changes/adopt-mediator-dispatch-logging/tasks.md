## 1. DispatchLoggingBehavior

- [x] 1.1 Add `DispatchLoggingBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>` under `src/Nagger.Host/Composition/Mediator/` that times `next` with a `Stopwatch`, logs success (message type + elapsed), and logs failures (`ValidationException` at Warning, not-found at Debug, others at Error) before rethrowing. Verify `dotnet build Nagger.slnx`.
- [x] 1.2 Register the behavior in `AddNaggerMediator` via `AddSingleton(typeof(IPipelineBehavior<,>), typeof(DispatchLoggingBehavior<,>))`. Verify `dotnet build Nagger.slnx`.

## 2. Remove hand-rolled middleware

- [x] 2.1 Remove the `Stopwatch` request-logging middleware from `src/Nagger.Host/Program.cs` (the `app.Use(...)` block). Verify `dotnet build Nagger.slnx`.

## 3. AppLog and exception handler

- [x] 3.1 In `src/Nagger.Host/AppLog.cs`, remove `RequestCompleted`, `ValidationRejected`, and `UnexpectedFailure`; add source-generated log messages for dispatch success (Info), validation failure (Warning), not-found (Debug), and failure (Error). Keep `TaskCreated`. Verify `dotnet build Nagger.slnx`.
- [x] 3.2 In `src/Nagger.Host/Api/ExceptionHandling/ApiExceptionHandler.cs`, replace the `ValidationError` write with `Results.ValidationProblem(...)` for `ValidationException`, remove the now-redundant log calls, and delete `src/Nagger.Host/Api/ValidationError.cs`. Verify `dotnet build Nagger.slnx`.

## 4. Tests

- [x] 4.1 Remove the `RequestCompleted_GivenTaskRequest_WhenLogged_ThenDoesNotIncludeTaskContent`, `ValidationRejected_GivenTaskRequest_WhenLogged_ThenDoesNotIncludeTaskContent`, and `UnexpectedFailure_GivenTaskRequest_WhenLogged_ThenDoesNotIncludeTaskContent` tests from `tests/Nagger.Host.Tests/ApiTests.cs`. Verify `dotnet test tests/Nagger.Host.Tests`.
- [x] 4.2 Run `dotnet test tests/Nagger.Host.Tests` and confirm the validation-error tests (e.g. `CreateOneShotTask_GivenInvalidPayload_WhenCreateRequested_ThenReturnsValidationErrorsWithoutPersistingTask`) still pass against the `errors` property.
- [x] 4.3 Add log tests for `DispatchSucceeded`, `DispatchValidationFailed`, `DispatchNotFound`, and `DispatchFailed` in `tests/Nagger.Host.Tests/ApiTests.cs`, mirroring the existing `TaskCreated_GivenTaskIdentifier_WhenLogged_ThenDoesNotIncludeTaskContent` pattern (assert the expected EventId and that the message does not include task content). Verify `dotnet test tests/Nagger.Host.Tests`.

## 5. Documentation

- [x] 5.1 Update the AGENTS.md "Host Organization" logging rule so it states that Mediator pipeline behavior owns dispatch diagnostics and no longer requires hand-rolled HTTP request logging. Verify the rule matches the implementation.

## 6. Full verification

- [x] 6.1 Run `dotnet test Nagger.slnx` and confirm the full suite passes.
