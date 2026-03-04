using CompanyName.MyMeetings.PerformanceTests.Models;

namespace CompanyName.MyMeetings.PerformanceTests.Interfaces;

public interface IBaselineRepository
{
    Task<BaselineMetrics?> GetBaselineAsync(string scenarioName);

    Task SaveBaselineAsync(string scenarioName, BaselineMetrics metrics);

    Task<ComparisonResult> CompareAsync(string scenarioName, MetricsSummary current);
}

public record BaselineMetrics(
    string ScenarioName,
    DateTime CapturedAt,
    TimeSpan P50ResponseTime,
    TimeSpan P95ResponseTime,
    TimeSpan P99ResponseTime,
    double AverageThroughput,
    double ErrorRate);

public record ComparisonResult(
    bool HasRegression,
    double P95ResponseTimeDiffPercent,
    double ThroughputDiffPercent,
    List<string> RegressionFlags);
