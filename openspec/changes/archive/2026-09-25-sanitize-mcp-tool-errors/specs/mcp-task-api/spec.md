## ADDED Requirements

### Requirement: Sanitize unexpected MCP tool failures
The MCP server SHALL catch unexpected exceptions raised while executing a tool and return a generic tool error whose message contains no internal detail — no exception message, type name, or stack trace — rather than propagating the exception to the client. The server SHALL log the failure server-side.

#### Scenario: Unexpected failure inside an MCP tool
- **WHEN** a tool invocation fails with an exception other than a validation error or a not-found error
- **THEN** the server returns a generic tool error containing no exception message, type name, or stack trace, and logs the failure server-side
