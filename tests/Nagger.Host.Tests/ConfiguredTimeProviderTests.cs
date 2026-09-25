using Microsoft.Extensions.Configuration;
using Nagger.Host.Infrastructure;
using Shouldly;

namespace Nagger.Host.Tests;

public sealed class ConfiguredTimeProviderTests
{
    [Fact]
    public void ConfiguredTimeProvider_GivenInvalidTimeZone_WhenConstructed_ThenThrowsMessageNamingValueAndKey()
    {
        var configuration = Configuration(new Dictionary<string, string?> { ["Nagger:TimeZone"] = "Invalid/Zone" });

        var exception = Should.Throw<InvalidOperationException>(() => new ConfiguredTimeProvider(configuration));

        exception.Message.ShouldContain("Invalid/Zone");
        exception.Message.ShouldContain("Nagger:TimeZone");
        exception.InnerException.ShouldBeOfType<TimeZoneNotFoundException>();
    }

    [Fact]
    public void ConfiguredTimeProvider_GivenValidTimeZone_WhenConstructed_ThenExposesConfiguredZone()
    {
        var configuration = Configuration(new Dictionary<string, string?> { ["Nagger:TimeZone"] = "Europe/Helsinki" });

        var provider = new ConfiguredTimeProvider(configuration);

        provider.LocalTimeZone.Id.ShouldBe("Europe/Helsinki");
    }

    private static IConfiguration Configuration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
