## 1. Editorconfig

- [x] 1.1 Add a root `.editorconfig` with `root = true` and `[*.cs] dotnet_diagnostic.IDE0130.severity = suggestion`; verify the file exists at the repository root with those two directives
- [x] 1.2 Run `dotnet build Nagger.slnx` and verify the build succeeds with no new warnings
- [x] 1.3 Run `dotnet test Nagger.slnx` and verify all tests pass
