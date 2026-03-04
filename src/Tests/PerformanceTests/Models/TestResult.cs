using CompanyName.MyMeetings.PerformanceTests.Interfaces;

namespace CompanyName.MyMeetings.PerformanceTests.Models;

public enum TestStatus
{
    Passed,
    Failed,
    Error
}

public record TestResult(
    string ScenarioName,
    DateTime StartTime,
    DateTime EndTime,
    TestStatus Status,
    MetricsSummary Metrics,
    List<ErrorDetail> Errors,
    List<ResourceViolation>? ResourceViolations = null,
    TimeSeriesData? TimeSeries = null,
    ComparisonResult? BaselineComparison = null);

public record ErrorDetail(
    DateTime Timestamp,
    string Endpoint,
    int StatusCode,
    string Message);
