## Purpose

Define focused behavioral coverage expectations for Host REST and MCP integration tests.

## Requirements

### Requirement: Host integration tests isolate observable behaviors
The Host test project MUST organize each REST and MCP integration test around one externally observable behavior. A test MAY assert multiple fields when they form the contract of that single outcome, but it MUST NOT combine unrelated endpoint, tool-discovery, lifecycle, validation, report, or logging behaviors.

#### Scenario: A REST behavior is reviewed
- **WHEN** a contributor inspects a REST integration test
- **THEN** its setup, action, and assertions describe one REST endpoint outcome or one logging outcome

#### Scenario: An MCP behavior is reviewed
- **WHEN** a contributor inspects an MCP integration test
- **THEN** its setup, action, and assertions describe one initialization, discovery, tool-call, error, or report outcome

### Requirement: Parameterized Host tests represent one behavior
The Host test project MUST use parameterized tests only when every case has the same behavior, setup shape, and assertion structure. A parameterized test MUST NOT branch between distinct expected outcomes.

#### Scenario: A lifecycle behavior has equivalent cases
- **WHEN** multiple lifecycle inputs have the same observable outcome and assertions
- **THEN** they may be represented as parameterized cases

#### Scenario: Lifecycle behaviors have distinct contract fields
- **WHEN** lifecycle inputs require distinct setup or assertions
- **THEN** they are represented by separate tests

### Requirement: Focused tests preserve Host contract coverage
The Host test project MUST restrict REST and MCP integration tests to four concerns: persistence and adapter wiring (that handlers, stores, and SQLite round-trip and project correctly), exception-to-response mapping (validation errors to 400, missing tasks to 404, unexpected failures to a sanitized 500, and MCP tool errors to an `isError` result with sanitized text), the wire contract (status codes, response field names and shapes, content types, and MCP protocol negotiation, tool discovery, and structured content), and operational logging (that log events carry the expected event IDs without leaking task content). Host tests MUST NOT re-assert domain rules owned by Core in-memory tests, including lifecycle transition outcomes, morning-report classification and ordering, recurrence computation, and validation rule messages.

#### Scenario: A domain rule changes
- **WHEN** a lifecycle transition, report classification, recurrence rule, or validation message changes
- **THEN** only Core in-memory tests change
- **AND** Host integration tests remain stable

#### Scenario: An adapter mapping changes
- **WHEN** a SQLite store or instance reader changes how it maps or filters persisted rows
- **THEN** a Host integration test verifies the round-trip and filtering

#### Scenario: An exception mapping changes
- **WHEN** an exception-to-response mapping changes
- **THEN** a Host integration test verifies the mapped status code and response body

#### Scenario: A wire contract changes
- **WHEN** an endpoint's status code, field name, response shape, content type, or MCP tool schema changes
- **THEN** a Host integration test verifies the contract

#### Scenario: A log event is emitted
- **WHEN** a log event is emitted during a task dispatch
- **THEN** a Host test verifies the event ID and that the message does not contain task content

#### Scenario: Host integration tests are run
- **WHEN** the Host test project executes
- **THEN** REST and MCP wiring, error-mapping, and wire-contract coverage passes without requiring a multi-behavior workflow test
