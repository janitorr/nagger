## Context

Nagger is an ASP.NET Core host with a REST adapter in `Api/`, a Mediator-based Core command/query layer, and SQLite infrastructure. The Core layer already owns task creation, lifecycle validation, report generation, and the ports needed by those operations. This change adds an MCP interface for clients that can discover and call tools, while retaining REST until a later MCP-only hosting migration.

## Goals / Non-Goals

**Goals:**
- Host a streamable-HTTP MCP server in the existing `Nagger.Host` application.
- Map all currently supported task write operations and the morning-report read operation to MCP tools.
- Reuse Core commands, queries, and domain exceptions rather than duplicating behavior in the MCP adapter.
- Return structured tool output suitable for MCP clients and clear tool errors for validation, transition, and missing-task failures.
- Preserve the REST API and its contracts.

**Non-Goals:**
- Replacing REST, changing the host process model, or adding stdio transport.
- Adding authentication, authorization, multi-user isolation, or remote deployment concerns.
- Extending task types, reminder scheduling, or persistence schema.

## Decisions

### Use streamable HTTP in the existing web host

Register the .NET MCP server package in `Nagger.Host`, configure its HTTP transport in composition, and map its endpoint from `Program.cs`. This provides MCP client compatibility without a second executable or a process-lifecycle migration. Stdio is the intended future option, but it is deferred so the adapter contract can be validated independently of hosting changes.

Alternative considered: start with an stdio-only server. This would couple the first MCP change to an executable and operational migration, making REST coexistence and HTTP-based integration testing harder.

### Keep MCP tools in a dedicated host adapter area

Implement MCP tool definitions and their input/output records outside `Api/`, under a dedicated MCP-oriented host namespace. Tool methods receive `IMediator` and a cancellation token, construct the existing Core request objects, and map returned domain records to transport DTOs. This keeps `Program.cs` to composition and makes HTTP endpoint code independent from MCP tooling.

Alternative considered: invoke existing endpoint handlers or share endpoint DTOs. Endpoint handlers are HTTP-specific and sharing their DTOs would couple MCP contracts to REST implementation details.

### Expose one tool per supported domain action

Publish `create_one_shot_task`, `complete_one_shot_task`, `pause_one_shot_task`, `resume_one_shot_task`, `cancel_one_shot_task`, and `get_morning_report`. Explicit actions are discoverable, preserve current lifecycle semantics, and avoid an opaque action enum that clients could misuse.

Alternative considered: a generic lifecycle tool with an action parameter. It would reduce tool count but weakens discovery and makes per-action descriptions less precise.

### Convert expected domain failures into MCP tool errors

The MCP adapter catches only known Core validation and missing-task exceptions, returning non-successful MCP tool results with structured error content. Unexpected exceptions remain unhandled for the host's normal diagnostics. This mirrors REST's distinction between caller errors and server failures without relying on HTTP status codes in the MCP contract.

Alternative considered: let all exceptions escape. This gives clients inconsistent protocol-level failures and obscures actionable input errors.

### Use explicit MCP response records

Define MCP response records that represent the same task and morning-report fields exposed by REST, including offset-qualified timestamps. Responses are serialized as structured JSON tool content, not prose, so clients can reliably consume values.

Alternative considered: return formatted text. Text is easier to display but is not a stable machine-readable tool contract.

## Risks / Trade-offs

- [The MCP SDK's ASP.NET Core API may differ from expected registration and tool-result conventions] -> Pin a compatible package version, consult its API at implementation time, and cover initialization and tool calls with host integration tests.
- [REST and MCP representations can drift] -> Keep MCP DTO mappings close to the adapter and assert equivalent persisted outcomes and report data in tests.
- [A public streamable-HTTP endpoint has no authentication in this local service] -> Document the local-only scope; authentication is explicitly deferred and must precede non-local exposure.
- [MCP error content can become a second validation contract] -> Preserve Core field names and messages where practical, and test invalid inputs and failed lifecycle transitions.

## Migration Plan

1. Add the MCP server dependency, composition registration, and endpoint while leaving REST registrations unchanged.
2. Implement and test all MCP tools against a temporary SQLite database using the existing host test pattern.
3. Document the MCP connection URL and tool scope.
4. Deploy as an additive change; existing REST consumers require no migration.
5. If rollback is needed, remove the MCP endpoint and package registration. No data migration is required because the adapter uses the existing schema and Core operations.

## Open Questions

None for this additive change. The future MCP-only migration will choose the final transport and client launch/configuration model.
