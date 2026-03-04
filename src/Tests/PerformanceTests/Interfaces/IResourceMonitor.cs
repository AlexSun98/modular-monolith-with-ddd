using CompanyName.MyMeetings.PerformanceTests.Models;

namespace CompanyName.MyMeetings.PerformanceTests.Interfaces;

public interface IResourceMonitor
{
    Task StartMonitoringAsync(CancellationToken cancellationToken);

    Task<ResourceSnapshot> GetCurrentSnapshotAsync();
}

public record ResourceSnapshot(
    double CpuPercent,
    long MemoryMB,
    Dictionary<string, int> DbConnections);
