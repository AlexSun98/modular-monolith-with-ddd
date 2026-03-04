using CompanyName.MyMeetings.PerformanceTests.Configuration;
using CompanyName.MyMeetings.PerformanceTests.Interfaces;
using CompanyName.MyMeetings.PerformanceTests.LoadGeneration;
using CompanyName.MyMeetings.PerformanceTests.Metrics;
using CompanyName.MyMeetings.PerformanceTests.Models;

namespace CompanyName.MyMeetings.PerformanceTests.Orchestration;

public class TestOrchestrator
{
    private readonly HttpClient _httpClient;
    private readonly List<ErrorDetail> _errors = new();

    public TestOrchestrator(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<TestResult> ExecuteTestAsync(
        ITestConfiguration config,
        CancellationToken cancellationToken = default)
    {
        // Validate configuration
        var validator = new ConfigurationValidator();
        var validationResult = validator.Validate(config);

        if (!validationResult.IsValid)
        {
            throw new InvalidOperationException(
                $"Configuration validation failed: {string.Join(", ", validationResult.Errors)}");
        }

        var startTime = DateTime.UtcNow;

        try
        {
            // Initialize components
            var metricsCollector = new MetricsCollector();
            var loadGenerator = new LoadGenerator(_httpClient);

            // Execute load test
            var loadResult = await loadGenerator.ExecuteAsync(config, metricsCollector, cancellationToken);

            var endTime = DateTime.UtcNow;

            // Get metrics summary
            var summary = metricsCollector.GetSummary();
            var timeSeries = metricsCollector.GetTimeSeries();

            // Evaluate success criteria
            var status = EvaluateSuccessCriteria(config.Criteria, summary);

            return new TestResult(
                ScenarioName: config.ScenarioName,
                StartTime: startTime,
                EndTime: endTime,
                Status: status,
                Metrics: summary,
                Errors: _errors,
                ResourceViolations: null,
                TimeSeries: timeSeries);
        }
        catch (Exception ex)
        {
            var endTime = DateTime.UtcNow;

            _errors.Add(new ErrorDetail(
                Timestamp: DateTime.UtcNow,
                Endpoint: "N/A",
                StatusCode: 0,
                Message: $"Test execution failed: {ex.Message}"));

            return new TestResult(
                ScenarioName: config.ScenarioName,
                StartTime: startTime,
                EndTime: endTime,
                Status: TestStatus.Error,
                Metrics: new MetricsSummary(0, 0, 0, 0, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, 0, new Dictionary<int, int>()),
                Errors: _errors);
        }
    }

    private TestStatus EvaluateSuccessCriteria(SuccessCriteria criteria, MetricsSummary summary)
    {
        var failures = new List<string>();

        // Check max response time (using p95)
        if (criteria.MaxResponseTime.HasValue && summary.P95ResponseTime > criteria.MaxResponseTime.Value)
        {
            failures.Add($"P95 response time {summary.P95ResponseTime.TotalMilliseconds}ms exceeds maximum {criteria.MaxResponseTime.Value.TotalMilliseconds}ms");
        }

        // Check min throughput
        if (criteria.MinThroughput.HasValue && summary.AverageThroughput < criteria.MinThroughput.Value)
        {
            failures.Add($"Average throughput {summary.AverageThroughput:F2} req/s is below minimum {criteria.MinThroughput.Value:F2} req/s");
        }

        // Check max error rate
        if (summary.ErrorRate > criteria.MaxErrorRate)
        {
            failures.Add($"Error rate {summary.ErrorRate:P2} exceeds maximum {criteria.MaxErrorRate:P2}");
        }

        if (failures.Count > 0)
        {
            foreach (var failure in failures)
            {
                _errors.Add(new ErrorDetail(
                    Timestamp: DateTime.UtcNow,
                    Endpoint: "Success Criteria",
                    StatusCode: 0,
                    Message: failure));
            }

            return TestStatus.Failed;
        }

        return TestStatus.Passed;
    }
}
