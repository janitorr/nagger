namespace Nagger.Host.Tests;

public sealed class FixedTimeProvider(DateTimeOffset utcNow, string timeZoneId) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
}
