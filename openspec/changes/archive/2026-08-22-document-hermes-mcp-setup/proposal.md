## Why

Nagger runs as a local Hermes MCP task server (one-shot + recurring reminders), but the Hermes-side wiring lives only in a private skill note: build the host dll, run it as a systemd user service, and point Hermes at the MCP endpoint. That setup is not reproducible by anyone else, or on a fresh machine, without hunting down those details.

## What Changes

- Add `docs/hermes-integration.md`, a step-by-step guide covering:
  - Building the host (framework-dependent `dotnet publish -c Release -o .release` as canonical, with a self-contained `--self-contained` fallback for machines without the .NET 10 runtime).
  - Running it server-style on the MCP endpoint, including a recommended systemd user unit with `Restart=always` and an explicit `WorkingDirectory`.
  - Where SQLite data lands (the app default `nagger.db` relative to the unit's `WorkingDirectory`, and how to redirect it via `Nagger__DatabasePath`).
  - Wiring it into Hermes via `~/.hermes/config.yaml` `mcp_servers`.
  - The stale MCP tool-discovery pitfall (restart the Hermes gateway after wiring or rebuilding).
- Link `docs/hermes-integration.md` from the MCP Server section of `USAGE.md`.

## Capabilities

### New Capabilities

<!-- None — documentation only, no spec-level behavior change. -->

### Modified Capabilities

<!-- None — documentation only. `skip_specs: true` is set in `.openspec.yaml`. -->

## Impact

- Documentation only: `docs/hermes-integration.md` (new) and `USAGE.md` (link added).
- No code, API, schema, or behavioral change to the MCP server or the task model.
