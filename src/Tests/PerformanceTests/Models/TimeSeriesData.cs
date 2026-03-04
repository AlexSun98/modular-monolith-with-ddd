namespace CompanyName.MyMeetings.PerformanceTests.Models;

public class TimeSeriesData
{
    public List<SecondBucket> Buckets { get; set; } = new();
}

public class SecondBucket
{
    public long EpochSecond { get; init; }

    public int RequestCount { get; set; }

    public int ErrorCount { get; set; }

    public double TotalResponseTime { get; set; }
}
