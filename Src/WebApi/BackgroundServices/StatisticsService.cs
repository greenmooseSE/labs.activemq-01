namespace WebApi.BackgroundServices;

internal class StatisticsService : IStatisticsService
{
    #region IStatisticsService members

    public int HealthInvokeCount => _healthInvokeCount;

    /// <inheritdoc />
    public void HealthInvoked()
    {
        Interlocked.Increment(ref _healthInvokeCount);
    }

    /// <inheritdoc />
    public void StatisticsServiceDoingWork()
    {
        Interlocked.Increment(ref _statisticsServiceWorkIterationCount);
    }

    public int StatisticsServiceWorkIterationCount => _statisticsServiceWorkIterationCount;

    #endregion

    #region Non-Public members

    private int _healthInvokeCount;
    private int _statisticsServiceWorkIterationCount;

    #endregion
}
