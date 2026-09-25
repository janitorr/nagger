## Context

`ConfiguredTimeProvider` (Host) overrides `TimeProvider.LocalTimeZone` with an expression-bodied property that calls `TimeZoneInfo.FindSystemTimeZoneById(configuration["Nagger:TimeZone"] ?? "Europe/Helsinki")` on every read. It is registered as a singleton via `AddSingleton<TimeProvider, ConfiguredTimeProvider>()` in `PersistenceServiceCollectionExtensions`. Core handlers (`MorningReportHandler`, the recurring handlers) consume the abstract `TimeProvider` and read `.LocalTimeZone` per item.

Two framework facts shape the approach: DI singletons are constructed lazily on first resolution (not at registration), and `TimeZoneInfo.FindSystemTimeZoneById` throws `TimeZoneNotFoundException` for an unknown id. `Program.cs` already validates startup state eagerly via `MigrateAsync`. The build now treats CA analyzer warnings as errors, so any new code must be analyzer-clean. See proposal.md for motivation.

## Goals / Non-Goals

**Goals:**

- Resolve the configured timezone once per process and reuse it.
- Surface an invalid `Nagger:TimeZone` as a startup failure with a message naming the value and the key.
- Keep the change Host-only; do not alter Core behavior.

**Non-Goals:**

- No change to `GetUtcNow()` (the class is a configured-*timezone* provider, not a clock).
- No Core call-site refactoring (e.g. hoisting `LocalTimeZone` out of the `MorningReport` loops).
- No new framework for configuration validation; raw `IConfiguration` stays.

## Decisions

1. **Cache the resolved zone in a constructor-set `readonly` field.**
   - *Why over alternatives:* A singleton's constructor runs exactly once, so resolving there guarantees single resolution. A field read is the cheapest possible `LocalTimeZone`. Hoisting at each call site would be redundant (and would require Core changes). A `Lazy<TimeZoneInfo>` adds machinery without benefit, since construction is already the single resolution point.
2. **Force resolution at startup with `app.Services.GetRequiredService<TimeProvider>()` in `Program.cs`, beside `MigrateAsync`.**
   - *Why:* Caching in the constructor alone does not fail fast — the singleton is built on first request. The explicit startup read converts the lazy construction into an eager, fail-fast validation, consistent with the existing `MigrateAsync` startup check. Placing it before the migration means a config error surfaces before any database work.
   - *Alternative rejected:* a standalone config-validation call would duplicate the id→zone resolution logic and risk drifting from the provider.
3. **Throw `InvalidOperationException` wrapping `TimeZoneNotFoundException`, with a message naming the value and the key.**
   - *Why:* `InvalidOperationException` is the idiomatic "bad startup configuration" signal for this codebase (no options/validation framework is used). Wrapping preserves the original as the inner exception. The message names both the offending value and `Nagger:TimeZone`, including the default `Europe/Helsinki` path.
4. **Align `FixedTimeProvider` (Host test double) to the same resolve-once-in-constructor pattern.**
   - *Why:* Consistency between the production provider and the test double; behavior is unchanged, so no existing tests are affected.
5. **Testing scope: unit-test `ConfiguredTimeProvider` construction (throws + message, and a valid-zone happy path).**
   - *Why:* The "resolved once" and "fails at startup" mechanics are framework behavior (DI laziness, `TimeZoneInfo` instance caching) and are exercised by the constructor cache plus the explicit startup read; no reflection or boot-failure integration test is added.

## Risks / Trade-offs

- **Startup now fails where it previously served requests** → intended; the failure message names the value and key so operators can fix it immediately, and it matches the existing fail-fast `MigrateAsync` behavior.
- **`TimeZoneNotFoundException` is only the realistic failure mode; `InvalidTimeZoneException` (corrupt tzdata) would still propagate unwrapped** → acceptable; the configured-value case is the documented one, and the inner exception still reaches the startup log.
- **The startup read is a bare `GetRequiredService` line in `Program.cs`** → kept minimal and beside the migration so its purpose is obvious; AGENTS.md's "keep Program.cs to composition/startup" guidance is respected.
