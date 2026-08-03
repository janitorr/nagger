## 1. Test Dependency Setup

- [x] 1.1 Add a pinned Shouldly package reference to the Core and Host test projects while retaining their xUnit framework and runner packages.

## 2. Assertion Conversion

- [x] 2.1 Convert behavioral assertions in `TaskFeatureTests.cs` to their Shouldly equivalents, preserving test scenarios and coverage.
- [x] 2.2 Convert behavioral assertions in `ApiTests.cs` to their Shouldly equivalents, preserving test scenarios and coverage.

## 3. Verification

- [x] 3.1 Confirm the Core and Host test source files contain no xUnit `Assert` behavioral assertions.
- [x] 3.2 Run `dotnet test Nagger.slnx` and resolve any assertion-semantic or dependency issues.
