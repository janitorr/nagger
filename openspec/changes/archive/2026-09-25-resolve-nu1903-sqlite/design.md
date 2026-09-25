## Context

`Microsoft.EntityFrameworkCore.Sqlite` 10.0.10 pulls a transitive `SQLitePCLRaw.bundle_e_sqlite3` 2.1.11, which carries `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 — inside the `<= 2.1.11` vulnerable range of [GHSA-2m69-gcr7-jv3q](https://github.com/advisories/GHSA-2m69-gcr7-jv3q) (HIGH). `dotnet list package --vulnerable --include-transitive` reports it, and every build emits `NU1903`.

Verified against the NuGet package metadata: EF Core 10.0.11 and 10.0.12 both raise their transitive `SQLitePCLRaw.bundle_e_sqlite3` / `SQLitePCLRaw.core` dependencies to `2.1.12`, whose `lib.e_sqlite3` is `2.1.12` — outside the vulnerable range. A scratch project referencing `Microsoft.EntityFrameworkCore.Sqlite` 10.0.12 reports "no vulnerable packages".

The earlier `address-analyzer-warnings` design noted a manual alternative — a top-level `SQLitePCLRaw.bundle_e_sqlite3` 2.1.13 reference (pulling `lib.e_sqlite3` 3.53.3) — and deliberately deferred the fix. A patched EF Core release now exists, so the manual pin is no longer the cheapest correct fix.

## Goals / Non-Goals

**Goals:**
- Clear `NU1903` by moving the transitive `SQLitePCLRaw.lib.e_sqlite3` out of the advisory's vulnerable range.
- Use the smallest, official change that achieves that (an EF Core patch bump), without introducing a direct dependency the code never references.

**Non-Goals:**
- No bump of `Microsoft.AspNetCore.Mvc.Testing` (separate ASP.NET Core testing package, no SQLite).
- No migration, schema, or application-code changes.
- No `NoWarn`/suppression of `NU1903` — the vulnerability is resolved, not silenced.
- Not introducing central package management or `Directory.Build.props` here (that belongs to the warnings-as-errors change, #51, which this unblocks).

## Decisions

### Decision: Bump EF Core to 10.0.12 rather than pin `SQLitePCLRaw.bundle_e_sqlite3` directly

**Chosen:** Raise `Microsoft.EntityFrameworkCore.Sqlite` and `Microsoft.EntityFrameworkCore.Design` from `10.0.10` to `10.0.12`. EF Core's own dependency update moves the transitive chain to patched `2.1.12`.

**Why:** It is the minimal change — two version strings, no new package, no native-binary jump. The advisory is range-based (`<= 2.1.11`), so `2.1.12` fully resolves it.

**Alternative considered (rejected):** a top-level `SQLitePCLRaw.bundle_e_sqlite3` 2.1.13 reference. This would pull `lib.e_sqlite3` 3.53.3 (a SQLite 3.x native bump), introduce a direct dependency the application never uses, and split the SQLitePCLRaw versioning across the transitive (EF-pinned) and direct references. Heavier than necessary once a patched EF Core exists.

**Alternative considered (rejected):** accept the risk with a recorded rationale and revisit date. Not justified when a patched release is available and the fix is a version bump.

### Decision: Target 10.0.12 (latest stable patch), not 10.0.11

Both 10.0.11 and 10.0.12 depend on `SQLitePCLRaw.bundle_e_sqlite3` 2.1.12, so either clears the advisory. 10.0.12 is the newest stable patch, so it is the natural target.

### Decision: Keep `Design` and `Sqlite` in lockstep

`Microsoft.EntityFrameworkCore.Design` is build tooling only (private assets), but it shares the EF Core release train. Pinning both to `10.0.12` avoids version drift between the runtime and design-time packages.

### Decision: Touch only the EF Core packages

`Microsoft.AspNetCore.Mvc.Testing` 10.0.10 in the Host test project is left unchanged — it is unrelated to the SQLite vulnerability and is not part of this change's scope.

## Risks / Trade-offs

- [EF Core 10.0.12 changes the SQLite provider behavior] → It is a patch release (same minor), and the solution has a full Core + Host test suite plus SQLite migration coverage that runs on the updated graph. `dotnet test Nagger.slnx` and a Host startup/migration check are the guard.
- [A later transitive dependency could reintroduce a vulnerability] → Not addressed here; the warnings-as-errors change (#51) and the standard `dotnet list package --vulnerable --include-transitive` check remain the ongoing guard.
- [Transitive `SQLitePCLRaw` 2.1.12 could itself later be flagged] → The advisory range is `<= 2.1.11`; if a future advisory covers 2.1.12, that is a separate follow-up, not this change.

## Migration Plan

- No data migration or schema change: SQLite file format and EF mappings are unaffected by a patch bump.
- Deploy: bump the versions, restore, run tests, start the host once to confirm migrations apply.
- Rollback: revert the two `.csproj` version strings to `10.0.10`; the database is untouched and unaffected.

## Open Questions

None.
