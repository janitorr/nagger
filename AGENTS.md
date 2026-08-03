# Nagger

## Commands

- Requires the .NET 10 SDK. Build the whole solution with `dotnet build Nagger.slnx`; run all tests with `dotnet test Nagger.slnx`.
- Run a focused test project with `dotnet test tests/Nagger.Core.Tests/Nagger.Core.Tests.csproj` or `dotnet test tests/Nagger.Host.Tests/Nagger.Host.Tests.csproj`. Both test projects use xUnit; host tests create and remove their own temporary SQLite database.
- Start the API with `dotnet run --project src/Nagger.Host`. The launch profile uses `http://localhost:5246`; without one, the host defaults to `http://127.0.0.1:5000`.

## Testing

- Name new or modified tests `Subject_GivenCondition_WhenAction_ThenOutcome`, for example `CreateTask_GivenOffsetTimestamp_WhenHandled_ThenPersistsTask`. Do not rename existing tests solely to apply this convention.

## Boundaries

- `src/Nagger.Core` owns task behavior and vertical feature slices in `Tasks/`. It must not reference ASP.NET Core, EF Core/SQLite, runtime configuration, or the system clock; add required dependencies as ports in `Ports.cs` and exercise them in Core tests.
- `src/Nagger.Host` is the HTTP/SQLite adapter and composition root. `Program.cs` maps endpoints and registers Mediator; `Infrastructure/` implements Core ports and owns EF mappings and migrations.
- EF migrations are applied automatically on Host startup. Keep schema changes, `NaggerDbContext` mappings, and migrations together under `src/Nagger.Host/Infrastructure/Migrations/`.

## Runtime And Contracts

- SQLite is the canonical local store. `Nagger:DatabasePath` defaults to `nagger.db`; `Nagger:TimeZone` defaults to `Europe/Helsinki`. Override configuration with standard .NET environment-variable keys such as `Nagger__DatabasePath` and `Nagger__TimeZone`.
- Persisted and API timestamps require ISO-8601 values with an explicit offset. Calculate report date boundaries in the configured IANA timezone, not UTC.
- Keep `GET /reports/morning` read-only. It must not change task, reminder, or timestamp state. The current endpoint contracts are specified in `openspec/specs/` and covered by Host integration tests.

## Specification Workflow

- Use the repository OpenSpec commands for planned work: `/opsx-propose`, `/opsx-apply`, `/opsx-sync`, and `/opsx-archive`. When applying a change, follow the CLI-provided context and update its `tasks.md` checkboxes as work completes.
