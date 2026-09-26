# Nagger

## Tech Stack

- .NET 10 (C#) with ASP.NET Core Minimal APIs, running on ARM64 Linux.
- `src/Nagger.Core` holds product behavior; `src/Nagger.Host` is the HTTP/SQLite adapter and composition root.
- EF Core with the SQLite provider; schema changes ship as versioned migrations applied on Host startup.
- Mediator dispatches Core commands and queries; a pipeline behavior owns dispatch diagnostics.
- The MCP endpoint uses the Model Context Protocol streamable-HTTP transport at `/mcp`.
- xUnit + Shouldly for tests, Stryker.NET for Core mutation testing, and CSharpier for formatting.

## Where To Look First

- Behavioral contracts: `openspec/specs/`.
- API endpoints, payloads, state transitions, and errors: `USAGE.md`.
- Deployment and Hermes wiring: `docs/hermes-integration.md`.
- Canonical feature pattern: follow an existing vertical slice in `src/Nagger.Core/Tasks/` (for example `ManageRecurringTaskLifecycle.cs`) and its Core tests.

## Commands

- Requires the .NET 10 SDK. Build the whole solution with `dotnet build Nagger.slnx`; run all tests with `dotnet test Nagger.slnx`.
- Run a focused test project with `dotnet test tests/Nagger.Core.Tests/Nagger.Core.Tests.csproj` or `dotnet test tests/Nagger.Host.Tests/Nagger.Host.Tests.csproj`. Both test projects use xUnit; host tests create and remove their own temporary SQLite database.
- Start the API with `dotnet run --project src/Nagger.Host`. The launch profile uses `http://localhost:5246`; without one, the host defaults to `http://127.0.0.1:5000`.
- Refer to [USAGE.md](USAGE.md) for the API endpoints, JSON payloads, state transitions, report semantics, and error responses.

## Testing

- Name new or modified tests `Subject_GivenCondition_WhenAction_ThenOutcome`. The action describes the domain operation, such as `WhenCompleteRequested`, rather than generic transport wording such as `WhenPosted`. Do not rename existing tests solely to apply this convention.
- Run Core mutation testing with `dotnet stryker` (config: `stryker-config.json`, mutates `Nagger.Core` against the Core tests). The run fails when the mutation score drops below 75% (`break` threshold); treat 80% (`high`) as the target. When adding or modifying feature code, run mutation testing and add tests for any newly surviving mutants.

## Formatting

- Code is formatted with CSharpier (120-column, see `.csharpierrc`). Run `dotnet csharpier format .` before committing any C# change.
- A `pre-commit` hook (enabled via `git config core.hooksPath .githooks`) runs `dotnet csharpier check .` and blocks commits with unformatted C#. Do not commit with `--no-verify` to bypass it; the CI build fails on unformatted code.

## Git And Pull Requests

- Merge pull requests by rebasing; never squash or create merge commits. Keep feature branches rebased on `origin/main` before pushing rather than merging `main` into them.

## Analyzer Warnings

- CA analyzer warnings fail the build, the same way formatting violations do. The root `Directory.Build.props` sets `CodeAnalysisTreatWarningsAsErrors=true`, scoped to code-analysis rules so NuGet (`NU*`) and compiler (`CS*`) warnings remain warnings.
- Analyzer settings (`AnalysisMode=Recommended`, `AnalysisLevel=latest`) are centralized in the same file and apply to every project, including the test projects.
- `CA1707` (underscores in member names) is disabled for test files in `.editorconfig`, because test names deliberately use underscores (`Subject_GivenCondition_WhenAction_ThenOutcome`).

## Boundaries

- `src/Nagger.Core` owns task behavior and vertical feature slices in `Tasks/`. It must not reference ASP.NET Core, EF Core/SQLite, runtime configuration, or the system clock; add required dependencies as ports in `Ports.cs` and exercise them in Core tests.
- `src/Nagger.Host` is the HTTP/SQLite adapter and composition root. `Infrastructure/` implements Core ports and owns EF mappings and migrations.
- EF migrations are applied automatically on Host startup. Keep schema changes, `NaggerDbContext` mappings, and migrations together under `src/Nagger.Host/Infrastructure/Migrations/`.

## Host Organization

- Keep `Program.cs` limited to application composition, middleware ordering, endpoint-group registration, database migration, and `app.Run()`.
- Group HTTP endpoint mappings and their contracts under `src/Nagger.Host/Api/` by API area. Register service groups through extension methods under `src/Nagger.Host/Composition/`.
- Centralize HTTP exception-to-response mapping with `IExceptionHandler`; endpoints must not catch domain exceptions to produce HTTP responses.
- Keep one infrastructure adapter concern per file, and name the file after its primary type, such as `SqliteTaskStore.cs` and `ConfiguredClock.cs`.
- Mediator pipeline behavior owns Core command/query dispatch diagnostics (message type, elapsed time, failure type) and replaces hand-rolled HTTP request logging.

## Runtime And Contracts

- SQLite is the canonical local store. `Nagger:DatabasePath` defaults to `nagger.db`; `Nagger:TimeZone` defaults to `Europe/Helsinki`. Override configuration with standard .NET environment-variable keys such as `Nagger__DatabasePath` and `Nagger__TimeZone`.
- Persisted and API timestamps require ISO-8601 values with an explicit offset. Calculate report date boundaries in the configured IANA timezone, not UTC.
- Keep `GET /reports/morning` read-only. It must not change task, reminder, or timestamp state. The current endpoint contracts are specified in `openspec/specs/` and covered by Host integration tests.

## Specification Workflow

- Use the repository OpenSpec commands for planned work: `/opsx-propose`, `/opsx-apply`, `/opsx-sync`, and `/opsx-archive`. When applying a change, follow the CLI-provided context and update its `tasks.md` checkboxes as work completes.
