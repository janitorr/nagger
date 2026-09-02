## 1. Core analyzer fixes

- [x] 1.1 Rename the `template` parameter to `recurringTemplate` on `IRecurringTaskTemplateStore.AddAsync`/`UpdateAsync` in `src/Nagger.Core/Tasks/Ports.cs`, and update the matching parameter in `SqliteRecurringTaskTemplateStore` and the two test fakes; verify `dotnet build Nagger.slnx` no longer reports CA1716.
- [x] 1.2 Replace `nameof(rule.Unit)` with `nameof(rule)` in `RecurrenceCalculator.CalculateNextDue` (`src/Nagger.Core/Tasks/Domain/RecurrenceCalculator.cs`); verify CA2208 is gone from the build.

## 2. Host analyzer fixes

- [x] 2.1 Rename the `context` parameter to `httpContext` (and its body references) in `ApiExceptionHandler.TryHandleAsync` (`src/Nagger.Host/Api/ExceptionHandling/ApiExceptionHandler.cs`); verify CA1725 is gone.
- [x] 2.2 Add `using System.Globalization;` and pass `CultureInfo.InvariantCulture` to the four `DateOnly.ToString("yyyy-MM-dd")` sites in `src/Nagger.Host/Api/RecurringTaskEndpoints.cs`, `src/Nagger.Host/Api/ReportEndpoints.cs`, and `src/Nagger.Host/Mcp/McpTaskTools.cs` (two sites); verify all CA1305 warnings are gone.
- [x] 2.3 Wrap the `AppLog.RequestCompleted(...)` call in `src/Nagger.Host/Program.cs` with `if (app.Logger.IsEnabled(LogLevel.Information))`; verify CA1873 is gone.

## 3. Verification

- [x] 3.1 Run `dotnet build Nagger.slnx` and confirm no code-analysis warnings remain (the `NU1903` SQLitePCLRaw warning is expected and out of scope for this change).
- [x] 3.2 Run `dotnet test Nagger.slnx` and confirm all tests pass.
