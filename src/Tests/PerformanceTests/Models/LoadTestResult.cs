namespace CompanyName.MyMeetings.PerformanceTests.Models;

public class LoadTestResult
{
    public int TotalRequests { get; set; }

    public int SuccessfulRequests { get; set; }

    public int FailedRequests { get; set; }

    public List<RequestMetric> Metrics { get; set; } = new();
}
