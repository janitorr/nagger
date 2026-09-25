## 1. Analyzer Gate

- [x] 1.1 Add a root `Directory.Build.props` setting `AnalysisMode=Recommended`, `AnalysisLevel=latest`, and `CodeAnalysisTreatWarningsAsErrors=true`, confirming the gate surfaces test-project warnings (230 CA1707/CA1305 errors, all in test code, 0 in src)
- [x] 1.2 Add a `.editorconfig` section suppressing `CA1707` for test files (underscore test names are the mandated convention) and verify the 220 `CA1707` errors disappear from `dotnet build Nagger.slnx`
- [x] 1.3 Fix the 10 `CA1305` call sites in test code using invariant parsing per the established src pattern (`ParseExact(..., CultureInfo.InvariantCulture)`) and verify they drop out of the build output
- [x] 1.4 Remove the duplicated `AnalysisMode`/`AnalysisLevel` properties from `src/Nagger.Core/Nagger.Core.csproj` and `src/Nagger.Host/Nagger.Host.csproj`
- [x] 1.5 Verify `dotnet build Nagger.slnx` succeeds with 0 warnings and 0 errors

## 2. Documentation

- [x] 2.1 Add an analyzer-gate note to `AGENTS.md` next to the Formatting/CSharpier section, including the test-file `CA1707` carve-out, and verify it states that CA analyzer warnings fail the build
- [x] 2.2 Update `design.md` to correct the earlier "pre-verified clean" claim (incremental build false-negative) and document the test-project carve-out decision

## 3. Verification

- [x] 3.1 Temporarily introduce a CA warning (e.g. `CA1822`) in `src/Nagger.Core` and verify `dotnet build Nagger.slnx` fails with that rule reported as an error, then revert the change
- [x] 3.2 Confirm the gate is scoped to code-analysis rules — a deliberate compiler warning (e.g. unused local, `CS0219`) in `src/Nagger.Core` must NOT fail the build — proving `NU*`/`CS*` warnings stay warnings
- [x] 3.3 Run `dotnet test Nagger.slnx` and verify all Core and Host tests pass with the gate enabled
- [x] 3.4 Run `dotnet csharpier check .` and verify it exits 0
