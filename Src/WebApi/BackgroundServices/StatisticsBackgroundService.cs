namespace WebApi.BackgroundServices;

public class StatisticsBackgroundService : BackgroundService
{
    #region Public members

    public StatisticsBackgroundService(ILogger<StatisticsBackgroundService> log, IStatisticsService statisticsService)
    {
        _log = log;
        _statisticsService = statisticsService;
    }

    #endregion

    #region Non-Public members

    private readonly ILogger<StatisticsBackgroundService> _log;
    private readonly IStatisticsService _statisticsService;

    public static event Action? OnIteration;

    public static TimeSpan IterationDelay { get; set; }=TimeSpan.FromMilliseconds(500);

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
            OnIteration?.Invoke();
            _log.LogDebug("{serviceName} is doing work with iteration delay {IterationDelay}.", serviceName,IterationDelay);
            _statisticsService.StatisticsServiceDoingWork();
            await Task.Delay(IterationDelay, stoppingToken);
        }
    }

    #endregion
}
