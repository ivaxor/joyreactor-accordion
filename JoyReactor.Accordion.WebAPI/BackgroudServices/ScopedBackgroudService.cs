using JoyReactor.Accordion.WebAPI.Extensions;

namespace JoyReactor.Accordion.WebAPI.BackgroudServices;

public abstract class ScopedBackgroudService : BackgroundService
{
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        if (ServiceCollectionExtensions.ServiceScopes.TryRemove(GetType(), out var serviceScope))
            serviceScope.Dispose();
    }

    public override void Dispose()
    {
        if (ServiceCollectionExtensions.ServiceScopes.TryRemove(GetType(), out var serviceScope))
            serviceScope.Dispose();

        base.Dispose();
    }
}