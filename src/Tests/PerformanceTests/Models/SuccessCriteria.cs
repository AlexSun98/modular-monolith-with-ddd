namespace CompanyName.MyMeetings.PerformanceTests.Models;

public record SuccessCriteria(
    TimeSpan? MaxResponseTime,
    double? MinThroughput,
    double MaxErrorRate);
