using System.Collections.Concurrent;

namespace JoyReactor.Accordion.WebAPI.Extensions;

public static class ServiceCollectionExtensions
{
    internal static ConcurrentDictionary<Type, IServiceScope> ServiceScopes = new ConcurrentDictionary<Type, IServiceScope>();

    public static IServiceCollection AddScopedHostedService<THostedService>(this IServiceCollection services)
        where THostedService : class, IHostedService
    {
        services.AddScoped<THostedService>();
        services.AddHostedService(serviceProvider =>
        {
            var serviceScope = serviceProvider.CreateScope();
            ServiceScopes.TryAdd(typeof(THostedService), serviceScope);
            return serviceScope.ServiceProvider.GetRequiredService<THostedService>();
        });

        return services;
    }
}