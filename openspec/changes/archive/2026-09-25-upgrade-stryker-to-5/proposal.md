## Why

Stryker.NET 5.0.0 was released on 2026-09-11; the repo pins `dotnet-stryker` at 4.16.0. The 5.0 line's only relevant breaking change is a .NET 10 runtime requirement, which this repo already meets (targets `net10.0`, CI installs `10.0.x`, local prereq is the .NET 10 SDK). Upgrading keeps the mutation engine current without any config or workflow churn.

## What Changes

- Bump `dotnet-stryker` from `4.16.0` to `5.0.0` in `.config/dotnet-tools.json`.
- No changes to `stryker-config.json`, the CI `mutation-core` job, or `DEVELOPMENT.md` — the config schema, reporters, and CLI flags are unchanged in 5.0, and the runtime requirement is already satisfied.
- Verify the mutation score still clears the 75% `break` threshold by running `dotnet stryker`.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This is a tooling/dependency bump with no observable change to the REST/MCP contracts or the Core task domain. (`skip_specs: true`.)

## Impact

- `.config/dotnet-tools.json` (the only file changed)
- `.github/workflows/dotnet.yml` — `mutation-core` job unaffected (`dotnet-version: 10.0.x` already provides the runtime)
- `StrykerOutput/` — regenerated on the local verification run
