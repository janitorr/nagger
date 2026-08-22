using Microsoft.Extensions.Configuration;

namespace Nagger.Host.Infrastructure;

public sealed class ConfiguredTimeProvider(IConfiguration configuration) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow;

    public override TimeZoneInfo LocalTimeZone =>
        TimeZoneInfo.FindSystemTimeZoneById(configuration["Nagger:TimeZone"] ?? "Europe/Helsinki");
}
