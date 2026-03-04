using System.Text.Json;
using System.Text.Json.Serialization;
using CompanyName.MyMeetings.PerformanceTests.Interfaces;
using CompanyName.MyMeetings.PerformanceTests.Models;

namespace CompanyName.MyMeetings.PerformanceTests.Reporting;

public class ReportGenerator : IReportGenerator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task GenerateJsonReportAsync(TestResult result, string outputPath)
    {
        try
        {
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var jsonReport = new JsonReport
            {
                ScenarioName = result.ScenarioName,
                StartTime = result.StartTime,
                EndTime = result.EndTime,
                Duration = result.EndTime - result.StartTime,
                Status = result.Status.ToString(),
                Summary = new JsonSummary
                {
                    TotalRequests = result.Metrics.TotalRequests,
                    SuccessfulRequests = result.Metrics.SuccessfulRequests,
                    FailedRequests = result.Metrics.FailedRequests,
                    ErrorRate = result.Metrics.ErrorRate,
                    P50ResponseTimeMs = result.Metrics.P50ResponseTime.TotalMilliseconds,
                    P95ResponseTimeMs = result.Metrics.P95ResponseTime.TotalMilliseconds,
                    P99ResponseTimeMs = result.Metrics.P99ResponseTime.TotalMilliseconds,
                    AverageThroughput = result.Metrics.AverageThroughput,
                    StatusCodeDistribution = result.Metrics.StatusCodeDistribution
                },
                Errors = result.Errors.Select(e => new JsonError
                {
                    Timestamp = e.Timestamp,
                    Endpoint = e.Endpoint,
                    StatusCode = e.StatusCode,
                    Message = e.Message
                }).ToList(),
                BaselineComparison = result.BaselineComparison != null ? new JsonBaselineComparison
                {
                    HasRegression = result.BaselineComparison.HasRegression,
                    P95ResponseTimeDiffPercent = result.BaselineComparison.P95ResponseTimeDiffPercent,
                    ThroughputDiffPercent = result.BaselineComparison.ThroughputDiffPercent,
                    RegressionFlags = result.BaselineComparison.RegressionFlags
                }
                : null
            };

            var json = JsonSerializer.Serialize(jsonReport, JsonOptions);
            await File.WriteAllTextAsync(outputPath, json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to generate JSON report: {ex.Message}", ex);
        }
    }

    public async Task GenerateHtmlReportAsync(TestResult result, string outputPath)
    {
        try
        {
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var template = HtmlReportTemplate.GetTemplate();

            // Generate time-series data for charts
            var timeSeriesJson = GenerateTimeSeriesJson(result.TimeSeries);

            // Replace placeholders
            var html = template
                .Replace("{{ScenarioName}}", result.ScenarioName)
                .Replace("{{Status}}", result.Status.ToString())
                .Replace("{{StatusClass}}", result.Status.ToString().ToLowerInvariant())
                .Replace("{{StartTime}}", result.StartTime.ToString("yyyy-MM-dd HH:mm:ss"))
                .Replace("{{EndTime}}", result.EndTime.ToString("yyyy-MM-dd HH:mm:ss"))
                .Replace("{{Duration}}", (result.EndTime - result.StartTime).ToString(@"hh\:mm\:ss"))
                .Replace("{{TotalRequests}}", result.Metrics.TotalRequests.ToString())
                .Replace("{{Throughput}}", result.Metrics.AverageThroughput.ToString("F2"))
                .Replace("{{ErrorRate}}", (result.Metrics.ErrorRate * 100).ToString("F2"))
                .Replace("{{P50}}", result.Metrics.P50ResponseTime.TotalMilliseconds.ToString("F2"))
                .Replace("{{P95}}", result.Metrics.P95ResponseTime.TotalMilliseconds.ToString("F2"))
                .Replace("{{P99}}", result.Metrics.P99ResponseTime.TotalMilliseconds.ToString("F2"))
                .Replace("{{TimeSeriesData}}", timeSeriesJson)
                .Replace("{{ErrorsSection}}", GenerateErrorsSection(result.Errors));

            await File.WriteAllTextAsync(outputPath, html);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to generate HTML report: {ex.Message}", ex);
        }
    }

    private string GenerateErrorsSection(List<ErrorDetail> errors)
    {
        if (errors.Count == 0)
        {
            return string.Empty;
        }

        var errorsHtml = @"<div class=""errors"">
            <div class=""chart-title"">Errors</div>";

        foreach (var error in errors)
        {
            errorsHtml += $@"
            <div class=""error-item"">
                <div class=""timestamp"">{error.Timestamp:yyyy-MM-dd HH:mm:ss}</div>
                <div><strong>{error.Endpoint}</strong> - Status {error.StatusCode}</div>
                <div>{error.Message}</div>
            </div>";
        }

        errorsHtml += "</div>";
        return errorsHtml;
    }

    private string GenerateTimeSeriesJson(TimeSeriesData? timeSeries)
    {
        if (timeSeries == null || timeSeries.Buckets.Count == 0)
        {
            return "{ labels: [], throughput: [], responseTime: [], errorRate: [] }";
        }

        var labels = new List<string>();
        var throughput = new List<double>();
        var responseTime = new List<double>();
        var errorRate = new List<double>();

        foreach (var bucket in timeSeries.Buckets)
        {
            var timestamp = DateTimeOffset.FromUnixTimeSeconds(bucket.EpochSecond);
            labels.Add(timestamp.ToString("HH:mm:ss"));

            throughput.Add(bucket.RequestCount);

            var avgResponseTime = bucket.RequestCount > 0
                ? bucket.TotalResponseTime / bucket.RequestCount
                : 0;
            responseTime.Add(avgResponseTime);

            var bucketErrorRate = bucket.RequestCount > 0
                ? (double)bucket.ErrorCount / bucket.RequestCount * 100
                : 0;
            errorRate.Add(bucketErrorRate);
        }

        var json = JsonSerializer.Serialize(
            new
        {
            labels,
            throughput,
            responseTime,
            errorRate
        },
            JsonOptions);

        return json;
    }
}

internal class JsonReport
{
    public string ScenarioName { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public TimeSpan Duration { get; set; }

    public string Status { get; set; } = string.Empty;

    public JsonSummary Summary { get; set; } = null!;

    public List<JsonError> Errors { get; set; } = new();

    public JsonBaselineComparison? BaselineComparison { get; set; }
}

internal class JsonSummary
{
    public int TotalRequests { get; set; }

    public int SuccessfulRequests { get; set; }

    public int FailedRequests { get; set; }

    public double ErrorRate { get; set; }

    public double P50ResponseTimeMs { get; set; }

    public double P95ResponseTimeMs { get; set; }

    public double P99ResponseTimeMs { get; set; }

    public double AverageThroughput { get; set; }

    public Dictionary<int, int> StatusCodeDistribution { get; set; } = new();
}

internal class JsonError
{
    public DateTime Timestamp { get; set; }

    public string Endpoint { get; set; } = string.Empty;

    public int StatusCode { get; set; }

    public string Message { get; set; } = string.Empty;
}

internal class JsonBaselineComparison
{
    public bool HasRegression { get; set; }

    public double P95ResponseTimeDiffPercent { get; set; }

    public double ThroughputDiffPercent { get; set; }

    public List<string> RegressionFlags { get; set; } = new();
}
