using Nagger.Core.Tasks;

namespace Nagger.Host.Infrastructure;

public sealed class ConfiguredClock(IConfiguration configuration) : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    public TimeZoneInfo TimeZone =>
        TimeZoneInfo.FindSystemTimeZoneById(configuration["Nagger:TimeZone"] ?? "Europe/Helsinki");
}
