## 1. Bump the tool

- [x] 1.1 Change `dotnet-stryker` from `4.16.0` to `5.0.0` in `.config/dotnet-tools.json`; verify `dotnet tool restore` succeeds and `dotnet stryker --version` reports `5.0.0`.

## 2. Verify mutation testing under 5.0

- [x] 2.1 Run `dotnet stryker --config-file stryker-config.json --output StrykerOutput` and verify it exits 0 with the Core mutation score at or above the 75% break threshold (80% target).
- [x] 2.2 If 5.0 introduces newly surviving mutants, add Core tests for them and re-run to confirm the score returns to at least 75%.
