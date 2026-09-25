## Why

`ConfiguredTimeProvider` re-resolves the timezone on every `LocalTimeZone` read, and an invalid `Nagger:TimeZone` value surfaces only as an opaque 500 on the first request that needs it. A misconfigured deployment should fail fast at startup with a message naming the offending value, not start up healthy and fail later at request time.

## What Changes

- `ConfiguredTimeProvider` resolves the configured timezone once in its constructor and caches it in a field, so `LocalTimeZone` is a field read instead of a repeated configuration lookup plus timezone-database lookup.
- An invalid `Nagger:TimeZone` value now throws `InvalidOperationException` (wrapping the underlying `TimeZoneNotFoundException`) at construction, with a message naming both the configured value and the `Nagger:TimeZone` key.
- `Program.cs` forces construction of the timezone provider at startup, next to the existing database migration, so an invalid timezone fails the process before it begins serving requests.
- The `FixedTimeProvider` Host test double is aligned to the same resolve-once-in-constructor pattern.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This is a Host-only robustness change with no observable change to the REST/MCP contracts or the Core task domain; the timezone is still resolved from the same configuration key and the same default. (`skip_specs: true`.)

## Impact

- `src/Nagger.Host/Infrastructure/ConfiguredTimeProvider.cs`
- `src/Nagger.Host/Program.cs`
- `tests/Nagger.Host.Tests/FixedTimeProvider.cs`
- New `tests/Nagger.Host.Tests/ConfiguredTimeProviderTests.cs`
- No Core changes; existing timezone-boundary tests in `Nagger.Core.Tests` and Host integration tests are unaffected.
