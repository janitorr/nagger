## Why

The repo has no `.editorconfig`, so code-style conventions are implicit and unenforced. The upcoming reorganization of `src/Nagger.Core/Tasks/` introduces a `Domain/` subfolder with a `Nagger.Core.Tasks.Domain` sub-namespace, which makes "namespace matches folder structure" an actual convention to uphold. Adding an editorconfig now establishes that convention before the reorganization that depends on it.

## What Changes

- Add a root `.editorconfig` with `root = true`.
- Enable the IDE0130 analyzer rule (namespace matches folder structure) at `suggestion` severity for `*.cs` files.
- Do not enforce code style in `dotnet build` yet (defer `EnforceCodeStyleInBuild`).

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This is a pure tooling/dev-convention change with no externally observable behavior change, so it opts out of specs via `skip_specs: true`.

## Impact

- New file: `.editorconfig` at the repository root.
- No application code, API, dependency, or database changes.
