using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nagger.Host;

namespace Nagger.Host.Tests;

public sealed class NaggerFactory : WebApplicationFactory<Program>
{
    internal static readonly DateTimeOffset ScenarioNow = new(2026, 8, 3, 6, 0, 0, TimeSpan.Zero);
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"nagger-{Guid.NewGuid():N}.db");
    private readonly Action<IServiceCollection>? _configureServices;

    public NaggerFactory(Action<IServiceCollection>? configureServices = null) =>
        _configureServices = configureServices;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(configuration =>
            configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["Nagger:DatabasePath"] = _databasePath }
            )
        );
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(new FixedTimeProvider(ScenarioNow, "Europe/Helsinki"));
        });
        if (_configureServices is not null)
            builder.ConfigureServices(_configureServices);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (File.Exists(_databasePath))
            File.Delete(_databasePath);
    }
}
