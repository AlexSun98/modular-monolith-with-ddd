using CompanyName.MyMeetings.PerformanceTests.Configuration;
using CompanyName.MyMeetings.PerformanceTests.Orchestration;
using CompanyName.MyMeetings.PerformanceTests.Reporting;
using NUnit.Framework;

namespace CompanyName.MyMeetings.PerformanceTests.Integration;

[TestFixture]
public class HtmlReportGenerationTest
{
    [Test]
    [Explicit("Requires external API access")]
    public async Task GenerateHtmlReport_ForRealLoadTest()
    {
        // Arrange
        var configPath = Path.Combine(
            Path.GetDirectoryName(typeof(HtmlReportGenerationTest).Assembly.Location)!,
            "SampleConfigs",
            "realistic-api-test.yaml");

        var loader = new ConfigurationLoader();
        var config = loader.LoadFromFile(configPath);

        using var httpClient = new HttpClient();
        var orchestrator = new TestOrchestrator(httpClient);

        // Act - Run load test
        var result = await orchestrator.ExecuteTestAsync(config);

        // Generate reports
        var reportGenerator = new ReportGenerator();
        var outputDir = Path.Combine(Path.GetTempPath(), "perf-test-reports");
        Directory.CreateDirectory(outputDir);

        var jsonPath = Path.Combine(outputDir, "test-report.json");
        var htmlPath = Path.Combine(outputDir, "test-report.html");

        await reportGenerator.GenerateJsonReportAsync(result, jsonPath);
        await reportGenerator.GenerateHtmlReportAsync(result, htmlPath);

        // Assert
        Assert.That(File.Exists(jsonPath), Is.True);
        Assert.That(File.Exists(htmlPath), Is.True);

        var htmlContent = await File.ReadAllTextAsync(htmlPath);
        Assert.That(htmlContent, Does.Contain("Realistic API Load Test"));
        Assert.That(htmlContent, Does.Contain("chart"));

        Console.WriteLine($"Reports generated:");
        Console.WriteLine($"JSON: {jsonPath}");
        Console.WriteLine($"HTML: {htmlPath}");
    }
}
