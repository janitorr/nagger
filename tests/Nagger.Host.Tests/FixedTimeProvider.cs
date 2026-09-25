namespace Nagger.Host.Tests;

public sealed class FixedTimeProvider(DateTimeOffset utcNow, string timeZoneId) : TimeProvider
{
    private readonly TimeZoneInfo _localTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

    public override DateTimeOffset GetUtcNow() => utcNow;

    public override TimeZoneInfo LocalTimeZone => _localTimeZone;
}
