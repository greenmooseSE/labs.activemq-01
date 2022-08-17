using System;
using System.Linq;

namespace WebApi.BackgroundServices;

public interface IStatisticsService
{
    void HealthInvoked();
    void StatisticsServiceDoingWork();
    int HealthInvokeCount { get; }
    int StatisticsServiceWorkIterationCount { get; }
}
