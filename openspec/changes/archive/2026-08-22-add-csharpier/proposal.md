## Why

The repo has no formatter, so C# code style is left to individual judgment and drifts over time. CSharpier is an opinionated, zero-config formatter that removes formatting decisions from authoring and review. Enforcing it in CI also protects the slow mutation-testing job: formatting must be correct before the expensive Stryker run is triggered.

## What Changes

- Add `csharpier` as a local .NET tool pinned in `.config/dotnet-tools.json` (alongside `dotnet-stryker`).
- Reformat the entire codebase once with `dotnet csharpier format .` (auto-generated EF `*.Designer.cs` and the model snapshot are skipped automatically).
- Add a fail-early `dotnet csharpier check .` step at the top of the CI `build` job so unformatted code stops the pipeline before restore/build/test.
- Gate the `mutation-core` job on `build` so Stryker is skipped when formatting (or the build) fails.
- Add `.git-blame-ignore-revs` so the one-time reformat does not pollute `git blame`.
- Document the formatting convention in `DEVELOPMENT.md`.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This is a pure tooling/dev-convention change with no externally observable behavior change, so it opts out of specs via `skip_specs: true`.

## Impact

- New tool entry in `.config/dotnet-tools.json`.
- New files: `.git-blame-ignore-revs`.
- Modified: `.github/workflows/dotnet.yml` (format check step + job dependency), `DEVELOPMENT.md` (formatting note), and every hand-written `.cs` file (mechanical reformat only).
- No application code, API, dependency, or database changes.
