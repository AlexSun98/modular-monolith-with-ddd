namespace CompanyName.MyMeetings.PerformanceTests.Models;

public record ResourceThresholds(
    double MaxCpuPercent = 90.0,
    long MaxMemoryMB = 2048,
    int MaxDbConnections = 100);

public record ResourceViolation(
    DateTime Timestamp,
    string ResourceType,
    double ActualValue,
    double ThresholdValue);
