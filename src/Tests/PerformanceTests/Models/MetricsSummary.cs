namespace CompanyName.MyMeetings.PerformanceTests.Models;

public record MetricsSummary(
    int TotalRequests,
    int SuccessfulRequests,
    int FailedRequests,
    double ErrorRate,
    TimeSpan P50ResponseTime,
    TimeSpan P95ResponseTime,
    TimeSpan P99ResponseTime,
    double AverageThroughput,
    Dictionary<int, int> StatusCodeDistribution);
