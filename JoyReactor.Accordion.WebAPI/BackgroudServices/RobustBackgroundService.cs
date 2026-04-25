using JoyReactor.Accordion.WebAPI.Models;
using Microsoft.Extensions.Options;

namespace JoyReactor.Accordion.WebAPI.BackgroudServices;

public abstract class RobustBackgroundService : BackgroundService
{
    private readonly IOptions<BackgroundServiceSettings> settings;
    private readonly ILogger<RobustBackgroundService> logger;

    protected readonly PeriodicTimer PeriodicTimer;
    protected abstract bool IsIndefinite { get; }
    protected abstract TimeSpan SubsequentRunDelay { get; }
    protected bool IsEnabled => settings.Value.ServiceNamesEnabled.TryGetValue(GetType().Name, out var enabled) && enabled == true;

    public RobustBackgroundService(
    IOptions<BackgroundServiceSettings> settings,
    ILogger<RobustBackgroundService> logger)
    {
        this.settings = settings;
        this.logger = logger;

        PeriodicTimer = new PeriodicTimer(SubsequentRunDelay);
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (!IsEnabled)
            return;

        do
        {
            using (logger.BeginScope(new Dictionary<string, object>() { { "TraceId", Guid.NewGuid() } }))
            {
                try
                {
                    await RunAsync(cancellationToken);
                    if (IsIndefinite)
                    {
                        logger.LogInformation("{BackgroundServiceName} background service succesfully ran. Next run is scheduled.", GetType().Name);
                    }
                    else
                    {
                        logger.LogInformation("{BackgroundServiceName} background service succesfully ran.", GetType().Name);
                        return;
                    }
                }
                catch (OperationCanceledException ex)
                {
                    logger.LogWarning(ex, "{BackgroundServiceName} background service run cancelled.", GetType().Name);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "{BackgroundServiceName} background service failed. Next run is scheduled.", GetType().Name);
                }
            }
        } while (await PeriodicTimer.WaitForNextTickAsync(cancellationToken));
    }

    protected abstract Task RunAsync(CancellationToken cancellationToken);
}