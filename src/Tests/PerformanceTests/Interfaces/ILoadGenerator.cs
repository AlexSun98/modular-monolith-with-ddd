using CompanyName.MyMeetings.PerformanceTests.Models;

namespace CompanyName.MyMeetings.PerformanceTests.Interfaces;

public interface ILoadGenerator
{
    Task<LoadTestResult> ExecuteAsync(
        ITestConfiguration config,
        IMetricsCollector metrics,
        CancellationToken cancellationToken);
}
