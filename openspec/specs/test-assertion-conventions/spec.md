## Purpose

Define the assertion-library conventions for automated tests.

## Requirements

### Requirement: Fluent assertion standard
The automated test suite MUST use Shouldly for behavioral assertions in the Core and Host test projects. xUnit MUST remain the test framework and runner.

#### Scenario: A test verifies an observed outcome
- **WHEN** a Core or Host test asserts a value, collection, exception, or boolean outcome
- **THEN** the test expresses that outcome through a Shouldly assertion

#### Scenario: Tests are discovered and executed
- **WHEN** the solution test suite runs
- **THEN** xUnit discovers and executes the tests while Shouldly supplies assertion behavior

### Requirement: Consistent existing test assertions
All behavioral assertions in the existing Core and Host test source files MUST conform to the fluent assertion standard after this change.

#### Scenario: Existing test files are reviewed after conversion
- **WHEN** the Core and Host test projects are inspected
- **THEN** they contain no xUnit `Assert` behavioral assertions
