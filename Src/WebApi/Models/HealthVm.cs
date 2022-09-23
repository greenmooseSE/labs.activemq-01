namespace WebApi.Models;

using WebApi.BackgroundServices;

public class HealthVm
{
    #region Public members

    public int HealthInvokeCount { get; set; }

    public int StatisticsServiceWorkIterationCount { get; set; }

    public HealthVm(IStatisticsService statisticsService)
    {
        HealthInvokeCount = statisticsService.HealthInvokeCount;
        StatisticsServiceWorkIterationCount = statisticsService.StatisticsServiceWorkIterationCount;
    }

    [Obsolete("Json ctor")]
    public HealthVm()
    {
        
    }

    #endregion
}
