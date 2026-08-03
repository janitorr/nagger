## Why

Nagger currently exposes its task features only through a JSON REST API, which requires AI clients to implement application-specific HTTP calls. Adding an MCP API lets MCP-compatible clients discover and invoke the same task capabilities through a standard tool interface while the application continues to use its existing Core behavior and persistence.

## What Changes

- Add a streamable-HTTP Model Context Protocol (MCP) server to the existing `Nagger.Host` process.
- Expose MCP tools for creating one-shot tasks, changing their lifecycle state, and reading morning task reports.
- Reuse existing Core commands and queries so MCP and REST calls have identical domain behavior, validation, persistence, and timezone handling.
- Preserve the current REST API during this change; moving to an MCP-only host is deferred.

## Capabilities

### New Capabilities
- `mcp-task-api`: Provide discoverable MCP tools for Nagger's supported one-shot task and morning-report operations over streamable HTTP.

### Modified Capabilities

None.

## Impact

- Affects `Nagger.Host` composition and adds an MCP adapter alongside `Api/` endpoint mappings.
- Adds the .NET MCP server dependency and MCP-focused host integration tests.
- Adds a streamable-HTTP MCP endpoint and documents how compatible clients connect to it.
