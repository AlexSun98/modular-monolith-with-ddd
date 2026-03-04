namespace CompanyName.MyMeetings.PerformanceTests.Models;

public record ResourceSample(
    DateTime Timestamp,
    double CpuPercent,
    long MemoryMB,
    Dictionary<string, int> DbConnections);
