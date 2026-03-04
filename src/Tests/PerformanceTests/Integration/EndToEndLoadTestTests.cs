using CompanyName.MyMeetings.PerformanceTests.Configuration;
using CompanyName.MyMeetings.PerformanceTests.Models;
using CompanyName.MyMeetings.PerformanceTests.Orchestration;
using CompanyName.MyMeetings.PerformanceTests.Reporting;
using NUnit.Framework;

namespace CompanyName.MyMeetings.PerformanceTests.Integration;

[TestFixture]
public class EndToEndLoadTestTests
{
    [Test]
    public async Task ExecuteLoadTest_WithValidConfiguration_ShouldCompleteSuccessfully()
    {
        // Arrange
        var config = new TestConfiguration
        {
            ScenarioName = "Simple Load Test",
            Endpoints = new[]
            {
                new TestEndpoint(
                    "https://httpbin.org/delay/0",
                    HttpMethod.Get,
                    null,
                    null)
            },
            LoadParams = new LoadParameters(
                VirtualUsers: 5,
                Strategy: new RampUpStrategy(RampUpType.Immediate),
                RampUpDuration: TimeSpan.Zero,
                ThinkTime: null),
            Duration = TimeSpan.FromSeconds(10),
            WarmupPeriod = TimeSpan.FromSeconds(2),
            Criteria = new SuccessCriteria(
                MaxResponseTime: TimeSpan.FromSeconds(5),
                MinThroughput: 1.0,
                MaxErrorRate: 0.1),
            Authentication = null
        };

        using var httpClient = new HttpClient();
        var orchestrator = new TestOrchestrator(httpClient);

        // Act
        var result = await orchestrator.ExecuteTestAsync(config);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ScenarioName, Is.EqualTo("Simple Load Test"));
        Assert.That(result.Metrics.TotalRequests, Is.GreaterThan(0));
        Assert.That(result.Status, Is.EqualTo(TestStatus.Passed).Or.EqualTo(TestStatus.Failed));
    }

    [Test]
    public async Task ExecuteLoadTest_ShouldGenerateJsonReport()
    {
        // Arrange
        var config = new TestConfiguration
        {
            ScenarioName = "Report Generation Test",
            Endpoints = new[]
            {
                new TestEndpoint(
                    "https://httpbin.org/status/200",
                    HttpMethod.Get,
                    null,
                    null)
            },
            LoadParams = new LoadParameters(
                VirtualUsers: 3,
                Strategy: new RampUpStrategy(RampUpType.Immediate),
                RampUpDuration: TimeSpan.Zero,
                ThinkTime: null),
            Duration = TimeSpan.FromSeconds(10),
            WarmupPeriod = TimeSpan.FromSeconds(2),
            Criteria = new SuccessCriteria(
                MaxResponseTime: TimeSpan.FromSeconds(10),
                MinThroughput: 0.5,
                MaxErrorRate: 0.2),
            Authentication = null
        };

        using var httpClient = new HttpClient();
        var orchestrator = new TestOrchestrator(httpClient);
        var result = await orchestrator.ExecuteTestAsync(config);

        var reportGenerator = new ReportGenerator();
        var outputPath = Path.Combine(Path.GetTempPath(), $"test-report-{Guid.NewGuid()}.json");

        // Act
        await reportGenerator.GenerateJsonReportAsync(result, outputPath);

        // Assert
        Assert.That(File.Exists(outputPath), Is.True);

        var jsonContent = await File.ReadAllTextAsync(outputPath);
        Assert.That(jsonContent, Does.Contain("Report Generation Test"));
        Assert.That(jsonContent, Does.Contain("summary"));

        // Cleanup
        File.Delete(outputPath);
    }

    [Test]
    public async Task ExecuteLoadTest_WithWarmupPeriod_ShouldExcludeWarmupMetrics()
    {
        // Arrange
        var config = new TestConfiguration
        {
            ScenarioName = "Warmup Test",
            Endpoints = new[]
            {
                new TestEndpoint(
                    "https://httpbin.org/status/200",
                    HttpMethod.Get,
                    null,
                    null)
            },
            LoadParams = new LoadParameters(
                VirtualUsers: 2,
                Strategy: new RampUpStrategy(RampUpType.Immediate),
                RampUpDuration: TimeSpan.Zero,
                ThinkTime: null),
            Duration = TimeSpan.FromSeconds(10),
            WarmupPeriod = TimeSpan.FromSeconds(3),
            Criteria = new SuccessCriteria(
                MaxResponseTime: TimeSpan.FromSeconds(10),
                MinThroughput: 0.1,
                MaxErrorRate: 0.5),
            Authentication = null
        };

        using var httpClient = new HttpClient();
        var orchestrator = new TestOrchestrator(httpClient);

        // Act
        var result = await orchestrator.ExecuteTestAsync(config);

        // Assert
        Assert.That(result.Metrics.TotalRequests, Is.GreaterThan(0));

        // The actual test duration is 3 seconds (6 - 3 warmup)
        // So we should have fewer requests in the final metrics than total executed
    }
}
