namespace WebApi.BackgroundServices;

using AmqpNetLite.Common;

public class MessageBackgroundService : BackgroundService
{
    #region Public members

    public MessageBackgroundService(ILogger<MessageBackgroundService> log, IMessageService messageService,
        IStatisticsService statisticsService)
    {
        _log = log;
        _messageService = messageService;
        _statisticsService = statisticsService;
    }

    #endregion

    #region Non-Public members

    private readonly ILogger<MessageBackgroundService> _log;
    private readonly IMessageService _messageService;
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
        await _messageService.ProcessMessagesAsync(stoppingToken);
    }

    #endregion
}
