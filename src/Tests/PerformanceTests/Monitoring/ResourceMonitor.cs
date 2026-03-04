using System.Diagnostics;
using CompanyName.MyMeetings.PerformanceTests.Interfaces;
using CompanyName.MyMeetings.PerformanceTests.Models;

namespace CompanyName.MyMeetings.PerformanceTests.Monitoring;

public class ResourceMonitor : IResourceMonitor
{
    private readonly IMetricsCollector _metricsCollector;
    private readonly Process _currentProcess;
    private readonly ResourceThresholds _thresholds;
    private readonly List<ResourceViolation> _violations = new();
    private Task? _monitoringTask;

    public ResourceMonitor(IMetricsCollector metricsCollector, ResourceThresholds? thresholds = null)
    {
        _metricsCollector = metricsCollector;
        _currentProcess = Process.GetCurrentProcess();
        _thresholds = thresholds ?? new ResourceThresholds();
    }

    public List<ResourceViolation> GetViolations() => _violations;

    public Task StartMonitoringAsync(CancellationToken cancellationToken)
    {
        _monitoringTask = Task.Run(
            async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var snapshot = await GetCurrentSnapshotAsync();
                    var sample = new ResourceSample(
                        Timestamp: DateTime.UtcNow,
                        CpuPercent: snapshot.CpuPercent,
                        MemoryMB: snapshot.MemoryMB,
                        DbConnections: snapshot.DbConnections);

                    _metricsCollector.RecordResourceSample(sample);

                    // Check for threshold violations
                    CheckThresholds(sample);

                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception)
                {
                    // Continue monitoring even if a sample fails
                }
            }
        },
            cancellationToken);

        return Task.CompletedTask;
        }

    public Task<ResourceSnapshot> GetCurrentSnapshotAsync()
    {
        try
        {
            // Refresh process info
            _currentProcess.Refresh();

            // Get CPU usage (approximate)
            var cpuPercent = GetCpuUsage();

            // Get memory usage in MB
            var memoryMB = _currentProcess.WorkingSet64 / (1024 * 1024);

            // Get database connections (placeholder - would need actual DB connection pool access)
            var dbConnections = GetDatabaseConnections();

            return Task.FromResult(new ResourceSnapshot(
                CpuPercent: cpuPercent,
                MemoryMB: memoryMB,
                DbConnections: dbConnections));
        }
        catch (Exception)
        {
            // Return empty snapshot on error
            return Task.FromResult(new ResourceSnapshot(
                CpuPercent: 0,
                MemoryMB: 0,
                DbConnections: new Dictionary<string, int>()));
        }
    }

    private double GetCpuUsage()
    {
        try
        {
            // Get total processor time
            var totalProcessorTime = _currentProcess.TotalProcessorTime.TotalMilliseconds;
            var processorCount = Environment.ProcessorCount;

            // This is a simplified CPU calculation
            // For accurate CPU usage, we'd need to track time between samples
            return Math.Min(100.0, (totalProcessorTime / 1000.0) / processorCount);
        }
        catch
        {
            return 0;
        }
    }

    private Dictionary<string, int> GetDatabaseConnections()
    {
        // Placeholder implementation
        // In a real implementation, this would query the connection pool metrics
        // from Entity Framework or the database provider
        return new Dictionary<string, int>
        {
            { "Administration", 0 },
            { "Meetings", 0 },
            { "Payments", 0 },
            { "Registrations", 0 },
            { "UserAccess", 0 }
        };
    }

    private void CheckThresholds(ResourceSample sample)
    {
        // Check CPU threshold
        if (sample.CpuPercent > _thresholds.MaxCpuPercent)
        {
            _violations.Add(new ResourceViolation(
                Timestamp: sample.Timestamp,
                ResourceType: "CPU",
                ActualValue: sample.CpuPercent,
                ThresholdValue: _thresholds.MaxCpuPercent));
        }

        // Check memory threshold
        if (sample.MemoryMB > _thresholds.MaxMemoryMB)
        {
            _violations.Add(new ResourceViolation(
                Timestamp: sample.Timestamp,
                ResourceType: "Memory",
                ActualValue: sample.MemoryMB,
                ThresholdValue: _thresholds.MaxMemoryMB));
        }

        // Check database connection thresholds
        foreach (var (module, connections) in sample.DbConnections)
        {
            if (connections > _thresholds.MaxDbConnections)
            {
                _violations.Add(new ResourceViolation(
                    Timestamp: sample.Timestamp,
                    ResourceType: $"DB Connections ({module})",
                    ActualValue: connections,
                    ThresholdValue: _thresholds.MaxDbConnections));
            }
        }
    }
}
