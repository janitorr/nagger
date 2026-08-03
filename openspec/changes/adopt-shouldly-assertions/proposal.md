## Why

The test suite currently uses xUnit assertion calls whose argument ordering and imperative style make behavioral expectations slower to scan. Adopting Shouldly gives assertions a consistent, fluent form that states the observed outcome directly.

## What Changes

- Add Shouldly to both Core and Host test projects.
- Standardize test assertions on Shouldly while retaining xUnit as the test framework and runner.
- Convert the existing test assertions to the standard style.
- Use Shouldly for all new and modified test assertions.

## Capabilities

### New Capabilities
- `test-assertion-conventions`: Defines the assertion-library standard for the automated test suite.

### Modified Capabilities

- None.

## Impact

- Test project package references in `tests/Nagger.Core.Tests` and `tests/Nagger.Host.Tests`.
- Existing unit and host integration test source files.
- No production code, HTTP contracts, persisted data, or runtime behavior changes.
