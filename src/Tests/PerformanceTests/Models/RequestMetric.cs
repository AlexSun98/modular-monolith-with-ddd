namespace CompanyName.MyMeetings.PerformanceTests.Models;

public record RequestMetric(
    DateTime Timestamp,
    TimeSpan ResponseTime,
    int StatusCode,
    string Endpoint,
    bool IsWarmup);
