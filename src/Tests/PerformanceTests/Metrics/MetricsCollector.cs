using System.Collections.Concurrent;
using CompanyName.MyMeetings.PerformanceTests.Interfaces;
using CompanyName.MyMeetings.PerformanceTests.Models;

namespace CompanyName.MyMeetings.PerformanceTests.Metrics;

public class MetricsCollector : IMetricsCollector
{
    private readonly ConcurrentBag<RequestMetric> _allMetrics = new();
    private readonly ConcurrentDictionary<long, SecondBucket> _timeSeries = new();
    private readonly ConcurrentDictionary<int, int> _statusCodes = new();
    private readonly ConcurrentBag<ResourceSample> _resourceSamples = new();

    public void RecordRequest(RequestMetric metric)
    {
        _allMetrics.Add(metric);

        // Update status code distribution
        _statusCodes.AddOrUpdate(metric.StatusCode, 1, (_, count) => count + 1);

        // Update time-series bucket
        var epochSecond = new DateTimeOffset(metric.Timestamp).ToUnixTimeSeconds();
        _timeSeries.AddOrUpdate(
            epochSecond,
            _ => new SecondBucket
            {
                EpochSecond = epochSecond,
                RequestCount = 1,
                ErrorCount = metric.StatusCode >= 400 ? 1 : 0,
                TotalResponseTime = metric.ResponseTime.TotalMilliseconds
            },
            (_, bucket) =>
            {
                bucket.RequestCount++;
                if (metric.StatusCode >= 400)
                {
                    bucket.ErrorCount++;
                }

                bucket.TotalResponseTime += metric.ResponseTime.TotalMilliseconds;
                return bucket;
            });
    }

    public void RecordResourceSample(ResourceSample sample)
    {
        _resourceSamples.Add(sample);
    }

    public MetricsSummary GetSummary()
    {
        // Filter out warmup metrics
        var actualMetrics = _allMetrics.Where(m => !m.IsWarmup).ToList();

        if (actualMetrics.Count == 0)
        {
            return new MetricsSummary(
                TotalRequests: 0,
                SuccessfulRequests: 0,
                FailedRequests: 0,
                ErrorRate: 0,
                P50ResponseTime: TimeSpan.Zero,
                P95ResponseTime: TimeSpan.Zero,
                P99ResponseTime: TimeSpan.Zero,
                AverageThroughput: 0,
                StatusCodeDistribution: new Dictionary<int, int>());
        }

        var totalRequests = actualMetrics.Count;
        var failedRequests = actualMetrics.Count(m => m.StatusCode >= 400);
        var successfulRequests = totalRequests - failedRequests;
        var errorRate = totalRequests > 0 ? (double)failedRequests / totalRequests : 0;

        // Calculate percentiles
        var responseTimes = actualMetrics.Select(m => m.ResponseTime.TotalMilliseconds).OrderBy(t => t).ToList();
        var p50 = CalculatePercentile(responseTimes, 50);
        var p95 = CalculatePercentile(responseTimes, 95);
        var p99 = CalculatePercentile(responseTimes, 99);

        // Calculate throughput
        var testDuration = CalculateTestDuration(actualMetrics);
        var averageThroughput = testDuration > 0 ? totalRequests / testDuration : 0;

        // Get status code distribution (excluding warmup)
        var statusCodeDist = new Dictionary<int, int>();
        foreach (var metric in actualMetrics)
        {
            if (statusCodeDist.ContainsKey(metric.StatusCode))
            {
                statusCodeDist[metric.StatusCode]++;
            }
            else
            {
                statusCodeDist[metric.StatusCode] = 1;
            }
        }

        return new MetricsSummary(
            TotalRequests: totalRequests,
            SuccessfulRequests: successfulRequests,
            FailedRequests: failedRequests,
            ErrorRate: errorRate,
            P50ResponseTime: TimeSpan.FromMilliseconds(p50),
            P95ResponseTime: TimeSpan.FromMilliseconds(p95),
            P99ResponseTime: TimeSpan.FromMilliseconds(p99),
            AverageThroughput: averageThroughput,
            StatusCodeDistribution: statusCodeDist);
    }

    public TimeSeriesData GetTimeSeries()
    {
        return new TimeSeriesData
        {
            Buckets = _timeSeries.Values.OrderBy(b => b.EpochSecond).ToList()
        };
    }

    private double CalculatePercentile(List<double> sortedValues, int percentile)
    {
        if (sortedValues.Count == 0)
        {
            return 0;
        }

        if (sortedValues.Count == 1)
        {
            return sortedValues[0];
        }

        var index = (percentile / 100.0) * (sortedValues.Count - 1);
        var lowerIndex = (int)Math.Floor(index);
        var upperIndex = (int)Math.Ceiling(index);

        if (lowerIndex == upperIndex)
        {
            return sortedValues[lowerIndex];
        }

        var lowerValue = sortedValues[lowerIndex];
        var upperValue = sortedValues[upperIndex];
        var fraction = index - lowerIndex;

        return lowerValue + ((upperValue - lowerValue) * fraction);
    }

    private double CalculateTestDuration(List<RequestMetric> metrics)
    {
        if (metrics.Count == 0)
        {
            return 0;
        }

        var minTime = metrics.Min(m => m.Timestamp);
        var maxTime = metrics.Max(m => m.Timestamp);
        var duration = (maxTime - minTime).TotalSeconds;

        // If all requests happened at the same time, return 1 second to avoid division by zero
        return duration > 0 ? duration : 1;
    }
}
