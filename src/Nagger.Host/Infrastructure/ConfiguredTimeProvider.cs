using Microsoft.Extensions.Configuration;

namespace Nagger.Host.Infrastructure;

public sealed class ConfiguredTimeProvider : TimeProvider
{
    private const string ConfigurationKey = "Nagger:TimeZone";
    private const string DefaultTimeZone = "Europe/Helsinki";

    private readonly TimeZoneInfo _localTimeZone;

    public ConfiguredTimeProvider(IConfiguration configuration)
    {
        var timeZoneId = configuration[ConfigurationKey] ?? DefaultTimeZone;
        try
        {
            _localTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException exception)
        {
            throw new InvalidOperationException(
                $"Invalid time zone '{timeZoneId}' for configuration key '{ConfigurationKey}'.",
                exception
            );
        }
    }

    public override DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow;

    public override TimeZoneInfo LocalTimeZone => _localTimeZone;
}
