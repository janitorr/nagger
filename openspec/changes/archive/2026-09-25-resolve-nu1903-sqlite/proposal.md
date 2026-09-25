## Why

Every build emits `NU1903` for a known-vulnerable transitive dependency, `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 (HIGH, [GHSA-2m69-gcr7-jv3q](https://github.com/advisories/GHSA-2m69-gcr7-jv3q)), pulled in through `Microsoft.EntityFrameworkCore.Sqlite` 10.0.10. It was deferred in the `address-analyzer-warnings` change and has since sat unowned in every build. A patched EF Core release now exists, so the fix is a minimal version bump rather than a manual pin or an accepted-risk writeup.

## What Changes

- Bump `Microsoft.EntityFrameworkCore.Sqlite` from `10.0.10` to `10.0.12` in `src/Nagger.Host` and `tests/Nagger.Host.Tests`.
- Bump `Microsoft.EntityFrameworkCore.Design` from `10.0.10` to `10.0.12` in `src/Nagger.Host` (kept in lockstep with the Sqlite package).
- EF Core 10.0.11+ raises its transitive `SQLitePCLRaw.bundle_e_sqlite3` dependency from `2.1.11` to `2.1.12`, moving `SQLitePCLRaw.lib.e_sqlite3` out of the advisory's `<= 2.1.11` vulnerable range.

No application code, API, migration, or schema changes. No new direct dependency is introduced.

## Capabilities

### New Capabilities

<!-- none -->

### Modified Capabilities

<!-- none — this is a dependency-version bump with no spec-level behavior change (skip_specs: true). -->

## Impact

- Affected files: `src/Nagger.Host/Nagger.Host.csproj`, `tests/Nagger.Host.Tests/Nagger.Host.Tests.csproj`.
- Dependency graph: transitive `SQLitePCLRaw.bundle_e_sqlite3` / `SQLitePCLRaw.lib.e_sqlite3` move `2.1.11 -> 2.1.12`; EF Core `10.0.10 -> 10.0.12`.
- Unblocks the analyzer-warnings-as-errors change (#51), which was gated on clearing `NU1903` first.
- `Microsoft.AspNetCore.Mvc.Testing` stays at `10.0.10`: it is a separate ASP.NET Core testing package unrelated to the SQLite vulnerability and is out of scope.
