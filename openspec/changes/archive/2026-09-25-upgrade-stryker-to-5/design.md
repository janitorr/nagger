## Context

`dotnet-stryker` is pinned at `4.16.0` in `.config/dotnet-tools.json` with `rollForward: false`. `stryker-config.json` mutates `Nagger.Core` against `Nagger.Core.Tests` with a `break` threshold of 75%. CI's `mutation-core` job runs `dotnet stryker --config-file stryker-config.json --output StrykerOutput` on PRs, gated on `build`. See proposal.md for motivation.

Stryker 5.0.0's breaking changes are: (1) the .NET 10 runtime requirement, and (2) the baseline disk provider now following the output path. The repo already targets `net10.0`, CI installs `dotnet-version: 10.0.x`, and the repo does not use baseline/dashboard, so neither breaking change requires action. No config-schema, reporter, or CLI changes ship in 5.0.

## Goals / Non-Goals

**Goals:**

- Move the mutation tool to the current 5.0 line with a one-line version bump.
- Confirm the mutation score still clears the 75% `break` threshold (80% target) under 5.0.

**Non-Goals:**

- No changes to `stryker-config.json` thresholds, mutators, or reporters.
- No changes to the CI workflow or `DEVELOPMENT.md` (both already assume .NET 10).
- No baseline/dashboard adoption.

## Decisions

1. **Pin `5.0.0` exactly**, not a `5.0.*` floating range.
   - *Why:* the manifest uses `rollForward: false` and pins every tool to an exact version; exact pinning keeps CI reproducible and matches the existing style.
2. **Change only `.config/dotnet-tools.json`**.
   - *Why:* the 5.0 config schema, reporters (`Progress`/`Markdown`/`Json`), and CLI flags (`--config-file`, `--output`) are unchanged; the runtime requirement is already satisfied by the existing .NET 10 SDK/runtime.
3. **Verify with a single local `dotnet stryker` run** rather than editing tests preemptively.
   - *Why:* 5.0 adds no new mutation operators, so the score should be stable; a real run is the honest check. Tests are only touched if a survivor is introduced by the engine itself.

## Risks / Trade-offs

- **The mutation score could shift** (5.0 changes timeout calculation to use actual mutant runtimes, and refines codegen) → mitigate by running `dotnet stryker`; if it drops below 75%, add tests for the newly-surviving mutants or pin `ignore-mutations`, then re-run.
- **The `$schema` URL in `stryker-config.json` points at the `master` branch schema** → cosmetic (editor-only IntelliSense); left unchanged unless it 404s, in which case update the URL in a follow-up.
