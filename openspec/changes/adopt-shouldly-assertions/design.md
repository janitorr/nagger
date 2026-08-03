## Context

The Core and Host test projects use xUnit 2.9.3 and its `Assert` API. The projects contain unit, integration, and MCP tests; their behavioral assertions use the same assertion API but have no declared assertion-style convention. This change introduces one assertion dependency across both test projects without changing the test framework, runner, application behavior, or API contracts.

## Goals / Non-Goals

**Goals:**
- Express test outcomes with fluent assertions that read from the observed value to its expected semantic state.
- Establish Shouldly as the single assertion API in both test projects.
- Preserve the existing test coverage and behavior while converting assertions.

**Non-Goals:**
- Redesign test fixtures, HTTP helpers, JSON parsing, or test case structure.
- Change production code or public contracts.
- Retain a mixed xUnit and Shouldly assertion style after conversion.

## Decisions

### Use Shouldly in both test projects

Add the same Shouldly package version to `Nagger.Core.Tests` and `Nagger.Host.Tests`, then replace xUnit assertion calls with their Shouldly equivalents. xUnit remains responsible for test discovery, execution, and `[Fact]`/`[Theory]` attributes.

This gives all tests one readable assertion dialect. Using Shouldly only for new tests or only in Core would leave competing styles and make review expectations unclear.

### Convert assertions without restructuring test scenarios

The conversion will preserve each test's arrange-act-assert flow, test data, and assertion coverage. Exception assertions will use Shouldly's asynchronous exception assertion support; collection, null, equality, containment, and boolean checks will use the corresponding fluent assertions.

This intentionally separates assertion readability from the distinct concern of reducing repeated JSON traversal in Host tests. Typed response contracts or JSON helpers can be considered independently later.

### Pin an explicit package version

Use an explicit Shouldly version in each test project, matching the repository's existing package-reference convention. A centrally managed dependency version is out of scope because the solution does not currently use central package management.

## Risks / Trade-offs

- [Semantic differences in exception or collection assertions] -> Review converted assertions individually and run the complete test suite to confirm preserved behavior.
- [A new test-only dependency increases restore surface] -> Limit the dependency to test projects and pin its version.
- [Large mechanical diff obscures unrelated behavior changes] -> Keep the change assertion-focused and avoid fixture or production refactoring.
