## 1. Tooling

- [x] 1.1 Run `dotnet tool install csharpier` and verify `.config/dotnet-tools.json` gains a `csharpier` entry at version 1.3.0 alongside `dotnet-stryker`

## 2. One-time reformat

- [x] 2.1 Run `dotnet tool restore && dotnet csharpier format .` and verify `dotnet csharpier check .` exits 0 afterward
- [x] 2.2 Verify no auto-generated files changed (no `*.Designer.cs` or `NaggerDbContextModelSnapshot.cs` in the diff); if any did, add a `.csharpierignore` and re-run

## 3. CI enforcement

- [x] 3.1 Add a `Restore tools` (`dotnet tool restore`) step and a `Check formatting` (`dotnet csharpier check .`) step to the top of the `build` job in `.github/workflows/dotnet.yml`, before `Restore dependencies`
- [x] 3.2 Add `needs: build` to the `mutation-core` job and verify the workflow YAML is valid (e.g. via `actionlint` or GitHub Actions parse)

## 4. Blame hygiene and docs

- [x] 4.1 Add `.git-blame-ignore-revs` containing the reformat commit hash and verify `git blame` on a reformatted file skips that commit
- [x] 4.2 Add a short "Formatting" section to `DEVELOPMENT.md` documenting `dotnet csharpier format .` and `dotnet csharpier check .`

## 5. Verification

- [x] 5.1 Run `dotnet build Nagger.slnx` and verify the build succeeds with no new warnings
- [x] 5.2 Run `dotnet test Nagger.slnx` and verify all tests pass
- [x] 5.3 Run `dotnet csharpier check .` and verify it exits 0 with no files reported
