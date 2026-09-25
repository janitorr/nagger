## 1. Resolve the timezone once and fail fast on invalid config

- [x] 1.1 Rework `ConfiguredTimeProvider` to resolve `Nagger:TimeZone` once in its constructor into a `readonly TimeZoneInfo` field, and return that field from `LocalTimeZone`; wrap `TimeZoneNotFoundException` in `InvalidOperationException` whose message names the configured value and the `Nagger:TimeZone` key. Verify `dotnet build Nagger.slnx` succeeds with no CA analyzer warnings.
- [x] 1.2 Add `app.Services.GetRequiredService<TimeProvider>()` in `Program.cs` beside the migration so the provider constructs at startup rather than at first request. Verify `dotnet build Nagger.slnx` succeeds and the resolution line sits before `app.Run()`.

## 2. Align the Host test double

- [x] 2.1 Change `FixedTimeProvider` to resolve its timezone once in the constructor and return the cached field from `LocalTimeZone`. Verify existing Host tests pass unchanged.

## 3. Tests

- [x] 3.1 Add `ConfiguredTimeProviderTests` with two cases: an invalid timezone throws `InvalidOperationException` whose message names the value and `Nagger:TimeZone`, and a valid timezone exposes the configured zone. Verify `dotnet test Nagger.slnx` passes.
- [x] 3.2 Confirm the existing timezone-boundary tests (Core `Uses_configured_timezone_for_date_boundary`, the Helsinki recurring-completion test, and Host tests using `FixedTimeProvider`) still pass. Verify `dotnet test Nagger.slnx` is green.

## 4. Formatting and final verification

- [x] 4.1 Run `dotnet csharpier format .` and verify `dotnet csharpier check .` passes.
- [x] 4.2 Run `dotnet build Nagger.slnx` and `dotnet test Nagger.slnx` and verify both are clean.
