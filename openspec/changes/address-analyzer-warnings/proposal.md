## Why

The two project files now enable the .NET code analyzers (`AnalysisMode=Recommended`, `AnalysisLevel=latest` in Host / `8.0` in Core). The first build surfaced nine new analyzer warnings across Core and Host. Addressing them keeps the build clean so future warnings remain actionable instead of being drowned out by pre-existing noise.

## What Changes

- Rename the `template` parameter on `IRecurringTaskTemplateStore` members to `recurringTemplate`, and update the implementations/fakes to match (CA1716 ×2).
- Pass `nameof(rule)` instead of `nameof(rule.Unit)` to the `ArgumentOutOfRangeException` in `RecurrenceCalculator.CalculateNextDue` (CA2208).
- Rename `ApiExceptionHandler.TryHandleAsync`'s `context` parameter to `httpContext` to match the `IExceptionHandler` interface (CA1725).
- Format dates with `CultureInfo.InvariantCulture` in the four `DateOnly.ToString("yyyy-MM-dd")` call sites across the Host API and MCP responses (CA1305 ×4).
- Guard the `RequestCompleted` log call in `Program.cs` with an `IsEnabled(LogLevel.Information)` check (CA1873).

The `NU1903` warning (vulnerable transitive `SQLitePCLRaw.lib.e_sqlite3` 2.1.11) is explicitly out of scope for this change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This is a pure code-quality change: no externally observable behavior, API shape, or spec-level requirement changes. Parameter renames and `InvariantCulture` date formatting preserve the exact current output (`yyyy-MM-dd`). This opts out of specs via `skip_specs: true`.

## Impact

- `src/Nagger.Core/Tasks/Ports.cs` — parameter rename on `IRecurringTaskTemplateStore`.
- `src/Nagger.Core/Tasks/Domain/RecurrenceCalculator.cs` — exception argument name.
- `src/Nagger.Host/Api/ExceptionHandling/ApiExceptionHandler.cs` — parameter rename.
- `src/Nagger.Host/Api/RecurringTaskEndpoints.cs`, `src/Nagger.Host/Api/ReportEndpoints.cs`, `src/Nagger.Host/Mcp/McpTaskTools.cs` — invariant-culture date formatting.
- `src/Nagger.Host/Program.cs` — log-level guard around `RequestCompleted`.
- `src/Nagger.Host/Infrastructure/SqliteRecurringTaskTemplateStore.cs` and the Core/Host test fakes — parameter rename to match the interface.
- No dependency, EF, migration, or MCP contract changes.
