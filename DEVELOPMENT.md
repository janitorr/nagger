# Developing Nagger

## Prerequisites

- .NET 10 SDK

## Run Locally

Start the host with its development launch profile:

```bash
dotnet run --project src/Nagger.Host
```

It listens on `http://localhost:5246`. To run without a launch profile, use:

```bash
dotnet run --project src/Nagger.Host --no-launch-profile
```

Without a launch profile, the default address is `http://127.0.0.1:5000`.

The host applies EF Core migrations on startup. Its SQLite database is created at the configured database path.

## Configuration

| Setting | Default | Environment variable |
| --- | --- | --- |
| `Nagger:DatabasePath` | `nagger.db` | `Nagger__DatabasePath` |
| `Nagger:TimeZone` | `Europe/Helsinki` | `Nagger__TimeZone` |

The configured IANA timezone determines the local calendar date used to classify each task in a morning report.

## Build And Test

Build the solution:

```bash
dotnet build Nagger.slnx
```

Run all tests:

```bash
dotnet test Nagger.slnx
```

Run a focused test project:

```bash
dotnet test tests/Nagger.Core.Tests/Nagger.Core.Tests.csproj
dotnet test tests/Nagger.Host.Tests/Nagger.Host.Tests.csproj
```

Host integration tests use temporary SQLite databases and do not require a running service.

## Formatting

Code is formatted with CSharpier (pinned as a local .NET tool) using a 120-column print width, configured in `.csharpierrc`. Format the codebase before committing:

```bash
dotnet csharpier format .
```

CI enforces formatting with `dotnet csharpier check .`; run it locally to verify:

```bash
dotnet csharpier check .
```

Mechanical reformat commits are listed in `.git-blame-ignore-revs` so they do not pollute `git blame`.

## Mutation Testing

Run mutation testing for Core behavior and its focused test suite:

```bash
dotnet tool restore
dotnet stryker --config-file stryker-config.json --output StrykerOutput
```

The command fails when the mutation score drops below 75%. Its HTML report is written to `StrykerOutput/reports/`.

## Architecture

`src/Nagger.Core` contains task behavior, vertical feature slices, and the persistence/time ports it requires. It does not depend on ASP.NET Core, EF Core, configuration, or the system clock.

`src/Nagger.Host` is the Minimal API adapter and composition root. It maps HTTP requests to Core features, provides SQLite and clock adapters, and owns EF Core mappings and migrations.

## API Reference

See the [API usage reference](USAGE.md) for endpoints, payloads, state transitions, report semantics, and error responses.
