# Host Integration Test Organization

## Purpose

Define the organization of Host integration tests by public contract.

## Requirements

### Requirement: Host integration tests are organized by public contract
The Host test project MUST keep REST endpoint integration tests and MCP protocol integration tests in separate, contract-named source files.

#### Scenario: A contributor locates REST endpoint coverage
- **WHEN** a contributor inspects Host integration tests for `/tasks` or `/reports` endpoint behavior
- **THEN** the tests are located in `ApiTests.cs` without MCP protocol test methods

#### Scenario: A contributor locates MCP coverage
- **WHEN** a contributor inspects Host integration tests for MCP initialization, tool invocation, or MCP error behavior
- **THEN** the tests and MCP-specific protocol helpers are located in `McpTests.cs`

### Requirement: Shared Host test fixture has an independent location
The reusable `NaggerFactory` fixture MUST be defined outside REST and MCP contract test files so both suites can use it without source-file ownership coupling.

#### Scenario: A contract test creates an application host
- **WHEN** a REST or MCP integration test creates a `NaggerFactory`
- **THEN** the fixture provides the same isolated temporary SQLite database and configured timezone behavior
