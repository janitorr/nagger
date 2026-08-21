## Context

The repository currently has no `.editorconfig` (no `.editorconfig`, `.editorconfig` parent, or `Directory.Build.props` anywhere). The .NET SDK globs `**/*.cs` for compilation, and no analyzer/formatting rules are enforced today. A follow-up change (`reorganize-task-domain-model`) will move domain types into `src/Nagger.Core/Tasks/Domain/` under a `Nagger.Core.Tasks.Domain` sub-namespace, making "namespace matches folder structure" a convention worth encoding before that move lands.

## Goals / Non-Goals

**Goals:**

- Establish `root = true` at the repository root so this is the single source of style config.
- Turn on the IDE0130 analyzer ("namespace does not match folder structure") for `*.cs` as an IDE hint.
- Keep the config minimal and scoped to the convention being introduced.

**Non-Goals:**

- No naming, formatting, or other style rules beyond namespace-matching.
- No build-time enforcement (`EnforceCodeStyleInBuild`) in this change.
- No CI gate on style failures.

## Decisions

### Decision: Minimal hand-written `.editorconfig`, not the `dotnet new editorconfig` template

The `dotnet new editorconfig` template emits ~100 lines of naming/formatting rules tuned for new code. Dropped onto the existing codebase it would flag a large set of pre-existing style (naming, accessibility modifiers, `Async` suffixes) that is unrelated to this change. A minimal file containing only the namespace rule keeps the change surgical.

- Alternative (rejected): full `dotnet new editorconfig`. Broad, noisy, and mixes unrelated style enforcement into a scoped change.

### Decision: Enable only IDE0130 at `suggestion` severity

IDE0130 is the Roslyn code-style analyzer for "namespace does not match folder structure". `suggestion` surfaces it as an IDE hint without failing anything. The current code already conforms (files under `Tasks/` use `Nagger.Core.Tasks`), so enabling it produces no new warnings, and it will remain satisfied after the reorganization if the domain files adopt `Nagger.Core.Tasks.Domain`.

- Alternative (rejected): `warning`/`error` severity. Would surface build noise and imply enforcement policy that is out of scope here.

### Decision: Defer `EnforceCodeStyleInBuild`

Code-style rules (IDE0xxx) only run during `dotnet build` when `EnforceCodeStyleInBuild` is set. Enforcing that is a CI-policy decision, not an editor-convention decision, so it is left for a separate change.

## Risks / Trade-offs

- [IDE0130 stays a hint and can be ignored] → Acceptable; it is documentation-by-lint for the convention, and enforcement is a deliberate non-goal.
- [A hand-written file may drift from a future broader style effort] → It is intentionally scoped; a future change can extend or replace it without conflict.
