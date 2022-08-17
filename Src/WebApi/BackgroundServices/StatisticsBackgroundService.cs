namespace WebApi.BackgroundServices;

public class StatisticsBackgroundService : BackgroundService
{
    #region Public members

    public StatisticsBackgroundService(ILogger<StatisticsBackgroundService> log)
    {
        _log = log;
    }

    #endregion

    #region Non-Public members

    private readonly ILogger<StatisticsBackgroundService> _log;

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var serviceName = GetType().Name;
        _log.LogDebug("{serviceName} is starting.", serviceName);
        stoppingToken.Register(() =>
        {
            _log.LogDebug("{serviceName} is stopping.", serviceName);
        });
        while (!stoppingToken.IsCancellationRequested)
        {
            _log.LogDebug("{serviceName} is doing work.", serviceName);
            await Task.Delay(TimeSpan.FromMilliseconds(200), stoppingToken);
        }
    }

    #endregion
}
