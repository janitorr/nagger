using Mediator;
using Nagger.Core.Tasks;
using Nagger.Core.Tasks.Domain;

namespace Nagger.Host.Composition.Mediator;

public static class MediatorServiceCollectionExtensions
{
    public static IServiceCollection AddNaggerMediator(this IServiceCollection services)
    {
        services.AddMediator(options =>
        {
            options.ServiceLifetime = ServiceLifetime.Scoped;
            options.Assemblies = [typeof(TaskItem)];
        });
        return services;
    }
}
