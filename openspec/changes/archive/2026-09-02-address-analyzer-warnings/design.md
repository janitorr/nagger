## Context

The `.csproj` files now enable `AnalysisMode=Recommended` (Core `AnalysisLevel=8.0`, Host `AnalysisLevel=latest`). The build surfaces nine warnings (listed in the proposal); the fix is mechanical, localized, and behavior-preserving. See proposal.md for motivation and scope.

## Goals / Non-Goals

**Goals:**

- Reach a clean build (zero analyzer warnings) for Core and Host.
- Preserve all externally observable behavior and output formats.

**Non-Goals:**

- `NU1903` (vulnerable transitive `SQLitePCLRaw.lib.e_sqlite3` 2.1.11) is out of scope. Note: it *is* fixable by adding a top-level `SQLitePCLRaw.bundle_e_sqlite3` 2.1.13 reference (which pulls `lib.e_sqlite3 3.53.3` / SQLite 3.53.3), but the user chose to defer it. No `NoWarn`/suppression is added for it in this change.
- No new analyzers beyond the settings already added; no `TreatWarningsAsErrors`.

## Decisions

**1. Rename `template` → `recurringTemplate` (CA1716)**

- The parameter name `template` collides with a reserved keyword in other .NET languages (CA1716).
- Chosen `recurringTemplate` because it mirrors the existing `IRecurringTaskInstanceStore` convention (`instance`, `recurringTaskId`) and stays domain-specific. `item` was considered but is too generic.
- The rename is applied to the interface (`Ports.cs`) and propagated to `SqliteRecurringTaskTemplateStore` and the two test fakes for coherence, even though CA1716 only fires on the interface members.
- Alternative: suppress CA1716 — rejected; the fix is trivial and clearer than a suppression.

**2. `ArgumentOutOfRangeException(nameof(rule))` (CA2208)**

- `nameof(rule.Unit)` yields `"Unit"`, which is a property, not a method parameter. The invalid input is the `rule` parameter (its `Unit` is outside the handled set), so `nameof(rule)` is correct.
- `completionDate` cannot be the out-of-range argument, so it is not a candidate.
- This is a defensive `_` arm on a `switch` expression; the exception message loses the `"Unit"` specificity, which is acceptable since the value cannot occur through normal flow.

**3. `CultureInfo.InvariantCulture` for date formatting (CA1305 ×4)**

- Change `DateOnly.ToString("yyyy-MM-dd")` → `DateOnly.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)` in `RecurringTaskEndpoints.cs`, `ReportEndpoints.cs`, and `McpTaskTools.cs` (two sites), adding `using System.Globalization;`.
- The format string has no culture-sensitive tokens, so output is unchanged; `InvariantCulture` documents intent (ISO date for API/MCP contract) and satisfies the analyzer.
- Alternative: suppress CA1305 — rejected; the explicit provider is the correct, minimal fix.

**4. Rename `context` → `httpContext` (CA1725)**

- Renames the parameter on `ApiExceptionHandler.TryHandleAsync` and all in-method references so it matches the `IExceptionHandler` interface declaration.
- Parameter names are not part of the CLR signature, so this is source-compatible for internal callers.

**5. `IsEnabled` guard around `RequestCompleted` (CA1873)**

- `AppLog.RequestCompleted` is a `[LoggerMessage]` source-generated method, so its arguments (`context.Request.Path`, `context.Response.StatusCode`, `timer.ElapsedMilliseconds`) are still evaluated eagerly at the call site even when Information logging is disabled.
- Wrap the call in `if (app.Logger.IsEnabled(LogLevel.Information))`. `LogLevel` is available via the Web SDK's implicit usings.
- Alternative: `SkipEnabledCheck` on the logger method — not applicable; the eager argument evaluation is the issue, not the internal check.

## Risks / Trade-offs

- **Parameter rename breaks named-argument callers** → Parameter names are not part of the signature; all in-repo callers/fakes are updated in the same change. Low risk.
- **CA2208 message change** → Only the `ParamName` of a never-thrown defensive exception changes; no behavior depends on it. Low risk.
- **`InvariantCulture` format change** → Output is byte-identical (`yyyy-MM-dd` has no culture-sensitive tokens), verified by existing Host integration tests. Zero risk.
- **`IsEnabled` guard** → Slightly duplicates the source generator's internal enabled check; behavior-preserving (log still emitted when Information is enabled).

## Migration Plan

None required — no schema, data, configuration, or API contract changes.
