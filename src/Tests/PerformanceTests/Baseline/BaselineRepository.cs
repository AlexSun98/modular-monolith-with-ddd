using System.Text.Json;
using CompanyName.MyMeetings.PerformanceTests.Interfaces;
using CompanyName.MyMeetings.PerformanceTests.Models;

namespace CompanyName.MyMeetings.PerformanceTests.Baseline;

public class BaselineRepository : IBaselineRepository
{
    private readonly string _baselineDirectory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public BaselineRepository(string? baselineDirectory = null)
    {
        _baselineDirectory = baselineDirectory ?? Path.Combine(Directory.GetCurrentDirectory(), "Baselines");

        if (!Directory.Exists(_baselineDirectory))
        {
            Directory.CreateDirectory(_baselineDirectory);
        }
    }

    public async Task<BaselineMetrics?> GetBaselineAsync(string scenarioName)
    {
        try
        {
            var filePath = GetBaselineFilePath(scenarioName);

            if (!File.Exists(filePath))
            {
                return null;
            }

            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<BaselineMetrics>(json, JsonOptions);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task SaveBaselineAsync(string scenarioName, BaselineMetrics metrics)
    {
        try
        {
            var filePath = GetBaselineFilePath(scenarioName);
            var json = JsonSerializer.Serialize(metrics, JsonOptions);
            await File.WriteAllTextAsync(filePath, json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to save baseline: {ex.Message}", ex);
        }
    }

    public async Task<ComparisonResult> CompareAsync(string scenarioName, MetricsSummary current)
    {
        var baseline = await GetBaselineAsync(scenarioName);

        if (baseline == null)
        {
            return new ComparisonResult(
                HasRegression: false,
                P95ResponseTimeDiffPercent: 0,
                ThroughputDiffPercent: 0,
                RegressionFlags: new List<string> { "No baseline found for comparison" });
        }

        // Calculate percentage differences
        var p95Diff = CalculatePercentageDiff(
            baseline.P95ResponseTime.TotalMilliseconds,
            current.P95ResponseTime.TotalMilliseconds);

        var throughputDiff = CalculatePercentageDiff(
            baseline.AverageThroughput,
            current.AverageThroughput);

        var regressionFlags = new List<string>();
        var hasRegression = false;

        // Check for response time regression (>20% increase)
        if (p95Diff > 20)
        {
            hasRegression = true;
            regressionFlags.Add($"P95 response time increased by {p95Diff:F2}% (baseline: {baseline.P95ResponseTime.TotalMilliseconds:F2}ms, current: {current.P95ResponseTime.TotalMilliseconds:F2}ms)");
        }

        // Check for throughput regression (>15% decrease)
        if (throughputDiff < -15)
        {
            hasRegression = true;
            regressionFlags.Add($"Throughput decreased by {Math.Abs(throughputDiff):F2}% (baseline: {baseline.AverageThroughput:F2} req/s, current: {current.AverageThroughput:F2} req/s)");
        }

        return new ComparisonResult(
            HasRegression: hasRegression,
            P95ResponseTimeDiffPercent: p95Diff,
            ThroughputDiffPercent: throughputDiff,
            RegressionFlags: regressionFlags);
    }

    private double CalculatePercentageDiff(double baseline, double current)
    {
        if (baseline == 0)
        {
            return 0;
        }

        return ((current - baseline) / baseline) * 100;
    }

    private string GetBaselineFilePath(string scenarioName)
    {
        var safeFileName = string.Join("_", scenarioName.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(_baselineDirectory, $"{safeFileName}.baseline.json");
    }
}
