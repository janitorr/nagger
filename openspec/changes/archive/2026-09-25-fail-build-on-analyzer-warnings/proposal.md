## Why

Both projects enable analyzers (`AnalysisMode=Recommended`, `AnalysisLevel=latest`), and the `address-analyzer-warnings` change cleaned up nine CA warnings. But nothing keeps them clean: no warnings-as-errors gate, no `Directory.Build.props`, and CI runs a plain `dotnet build --no-restore`, so a reintroduced CA warning still yields a green build. Formatting is enforced (CI `dotnet csharpier check .` plus a pre-commit hook), but analyzer hygiene has no equivalent gate, so the cleanup will erode.

## What Changes

- Add a root `Directory.Build.props` that centralizes `AnalysisMode=Recommended` and `AnalysisLevel=latest` (so the two test projects are covered by the same policy) and sets `CodeAnalysisTreatWarningsAsErrors=true`, making CA analyzer warnings fail the build.
- Add a test-scoped `CA1707` suppression to `.editorconfig` (test names deliberately use underscores per the repo naming convention) and fix the `CA1305` culture-sensitive parse call sites in test code with invariant parsing.
- Remove the now-duplicated `AnalysisMode`/`AnalysisLevel` properties from `src/Nagger.Core` and `src/Nagger.Host` project files, leaving `Directory.Build.props` as the single source of truth.
- Document the analyzer gate in `AGENTS.md` alongside the existing CSharpier/formatting rule.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This is a pure build/tooling change with no externally observable behavior change, so it opts out of specs via `skip_specs: true`.

## Impact

- New file: `Directory.Build.props` (repo root).
- Modified: `.editorconfig` (test-scoped `CA1707` carve-out), `tests/Nagger.Core.Tests/RecurringTaskFeatureTests.cs`, `tests/Nagger.Host.Tests/ApiTests.cs`, `tests/Nagger.Host.Tests/McpTests.cs` (`CA1305` fixes), `src/Nagger.Core/Nagger.Core.csproj`, `src/Nagger.Host/Nagger.Host.csproj` (remove duplicated analyzer properties), `AGENTS.md` (document the gate).
- CI: no workflow change — `dotnet build` imports `Directory.Build.props` automatically, so CA warnings fail the `build` job in CI too.
- No application code, API, dependency, or database changes.
