## Context

Both source projects set `AnalysisMode=Recommended` and `AnalysisLevel=latest` in their own `.csproj` files; the two test projects leave analyzers at the SDK default (`AnalysisMode=Default`), so they run a narrower rule set and are not covered by the same policy. `NU1903` was already cleared by the `resolve-nu1903-sqlite` change, so the earlier blocker on a blanket gate is gone. The CI `build` job runs `dotnet build --no-restore`, and a local pre-commit hook plus `dotnet csharpier check .` already enforce formatting.

## Goals / Non-Goals

**Goals:**

- Make CA analyzer warnings fail `dotnet build` locally and in CI.
- Apply one analyzer policy to all four projects (source and test).
- Scope the gate to code-analysis rules so NuGet (`NU*`) and compiler (`CS*`) warnings stay warnings.

**Non-Goals:**

- No blanket `TreatWarningsAsErrors` (would couple the build to transient NuGet and future compiler warnings).
- No change to `.editorconfig` IDE rule severities (`IDE0055=none`, `IDE0130=suggestion`) — IDE diagnostics are suggestions, not warnings, and are outside the "warnings fail the build" surface.
- No CI workflow restructuring; the existing `dotnet build` step already enforces the gate.

## Decisions

### Decision: `CodeAnalysisTreatWarningsAsErrors=true` rather than blanket `TreatWarningsAsErrors`

**Chosen:** `CodeAnalysisTreatWarningsAsErrors=true` in `Directory.Build.props`. This first-party property escalates code-analysis (`CA*`) warnings to errors via the NetAnalyzers `_warnaserror` globalconfig, while leaving `CS*` compiler and `NU*` NuGet warnings at their normal severity.

**Why:** It is precisely the "scope to CA rules, not blanket" outcome the issue asks for. Verified empirically against the .NET 10 SDK (10.0.112): with `CodeAnalysisTreatWarningsAsErrors=true`, sample `CA1311`/`CA1304`/`CA1822` warnings became errors while `CS8600`/`CS8602` stayed warnings.

**Alternatives considered (rejected):**

- `TreatWarningsAsErrors=true` + `WarningsNotAsErrors` for `NU*`: blanket; would also promote future compiler warnings and require maintaining an allow-list.
- `WarningsAsErrors=CA*;IDE*`: unsupported — MSBuild does not accept wildcard prefixes in `WarningsAsErrors` (verified: `MSB1006` on the CLI, and no-op in a props file).

### Decision: `Directory.Build.props` at the repo root, remove per-project duplicates

**Chosen:** Put `AnalysisMode=Recommended`, `AnalysisLevel=latest`, and `CodeAnalysisTreatWarningsAsErrors=true` in a root `Directory.Build.props` (imported by every project in the tree), and remove the duplicated `AnalysisMode`/`AnalysisLevel` from the two source `.csproj` files.

**Why:** The root props file is the natural single source of truth and, per the issue, is what brings the test projects under the same analyzer policy. Removing the duplicates prevents future drift between projects.

**Alternatives considered (rejected):** keeping the source-project properties and only adding `CodeAnalysisTreatWarningsAsErrors` centrally — leaves two sources of truth and requires touching both places on any future `AnalysisLevel` change.

### Decision: Test projects get `Recommended`/`latest` plus two deliberate carve-outs

**Chosen:** All four projects run `AnalysisMode=Recommended` + `AnalysisLevel=latest` from `Directory.Build.props`, and `CA1707` is disabled for test files via `.editorconfig` while the `CA1305` call sites in tests are fixed with invariant parsing.

**Why:** Enabling the gate surfaced 230 new analyzer errors — all in test code, none in `src/`. Two rules conflict with real test code:

- `CA1707` (220 errors): test names use underscores by the mandated convention (`Subject_GivenCondition_WhenAction_ThenOutcome` in `AGENTS.md`), which CA1707 flags. The rule is cosmetic, so it is suppressed for `tests/**/*.cs` via `.editorconfig`, keeping `src/` covered.
- `CA1305` (10 errors): `DateOnly.Parse(...)` and `DateTime.ToString("yyyy-MM-dd")` without an `IFormatProvider` in test setup code. These are genuine culture-sensitivity findings and are fixed with `ParseExact(..., CultureInfo.InvariantCulture)` / `ToString(..., CultureInfo.InvariantCulture)`, matching the pattern established in `src` by `address-analyzer-warnings`.

**Alternatives considered (rejected):**

- Leave test projects at the SDK default analysis mode (`AnalysisMode=Default`) and apply only the gate — weaker "same policy" than the issue asks for.
- Suppress both `CA1707` and `CA1305` for tests — leaves genuine culture-sensitivity findings silently suppressed.
- Rename underscore test methods to satisfy CA1707 — contradicts the mandated naming convention (220 renames).

**Note on verification method:** the original plan claimed the solution was pre-verified clean via `dotnet build -p:AnalysisMode=Recommended ...`. That check was a **false negative**: the `-p:` global properties did not force the test projects to recompile, so MSBuild's incremental build reused the stale (Default-mode) outputs and reported 0 warnings. Only the imported `Directory.Build.props` file change forced a real rebuild that surfaced the test errors. Analyzer-gate verification must use the props file (or a clean/`--no-incremental` build), not `-p:` overrides.

## Risks / Trade-offs

- [A future CA rule becomes a warning in a newer SDK and breaks the build] → This is the intended gate; the fix is to address the warning, and the failure message identifies the rule. This is the same maintenance surface formatting already imposes.
- [A transient NuGet warning would fail a blanket gate] → Avoided by scoping to `CodeAnalysisTreatWarningsAsErrors`; `NU*` warnings are intentionally not escalated.
- [Enabling `Recommended` on test projects surfaces new warnings] → Happened during this change: 220 `CA1707` (suppressed for `tests/**/*.cs` in `.editorconfig` — conflicts with the mandated underscore naming convention) and 10 `CA1305` (fixed with invariant parsing). Remediation is part of this change.
- [IDE analyzers are not escalated] → Intentional: IDE diagnostics are configured as suggestions, not warnings, and formatting is enforced by CSharpier, not IDE analyzers.

## Migration Plan

- No data or schema migration. Add `Directory.Build.props`, remove the two duplicated source-project properties, add the `CA1707` test carve-out to `.editorconfig`, fix the `CA1305` test call sites, document in `AGENTS.md`.
- Rollback: revert `Directory.Build.props`, `.editorconfig`, and the test-file edits; restore the two `.csproj` properties; the build returns to its previous (permissive) behavior.

## Open Questions

None.
