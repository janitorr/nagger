using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nagger.Core.Tasks;
using Nagger.Host.Infrastructure;

namespace Nagger.Host.Composition.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddNaggerPersistence(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddDbContext<NaggerDbContext>(options =>
            options.UseSqlite($"Data Source={configuration["Nagger:DatabasePath"] ?? "nagger.db"}")
        );
        services.AddScoped<ITaskStore, SqliteTaskStore>();
        services.RemoveAll<TimeProvider>();
        services.AddSingleton<TimeProvider, ConfiguredTimeProvider>();
        services.AddScoped<IRecurringTaskTemplateStore, SqliteRecurringTaskTemplateStore>();
        services.AddScoped<IRecurringTaskInstanceStore, SqliteRecurringTaskInstanceStore>();
        return services;
    }
}
