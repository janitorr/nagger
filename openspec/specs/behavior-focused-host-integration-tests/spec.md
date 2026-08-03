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
The focused Host test suite MUST preserve coverage for REST task creation, validation, lifecycle operations, morning reports, exception mapping, and operational logging, and for MCP initialization, discovery, task creation, lifecycle operations, validation errors, missing-task errors, invalid transitions, and morning reports.

#### Scenario: Host integration tests are run
- **WHEN** the Host test project executes
- **THEN** REST and MCP contract coverage passes without requiring a multi-behavior workflow test
