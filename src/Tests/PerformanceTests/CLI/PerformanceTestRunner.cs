using CompanyName.MyMeetings.PerformanceTests.Configuration;
using CompanyName.MyMeetings.PerformanceTests.Models;
using CompanyName.MyMeetings.PerformanceTests.Orchestration;
using CompanyName.MyMeetings.PerformanceTests.Reporting;

namespace CompanyName.MyMeetings.PerformanceTests.CLI;

public class PerformanceTestRunner
{
    public async Task<int> RunAsync(string[] args)
    {
        try
        {
            // Parse command-line arguments
            var options = ParseArguments(args);

            if (options == null)
            {
                PrintUsage();
                return ExitCodes.ConfigurationError;
            }

            // Load configuration
            var loader = new ConfigurationLoader();
            var config = loader.LoadFromFile(options.ConfigFilePath);

            // Create HTTP client
            using var httpClient = new HttpClient
            {
                BaseAddress = new Uri(options.BaseUrl ?? "http://localhost:5000")
            };

            // Execute test
            var orchestrator = new TestOrchestrator(httpClient);
            var result = await orchestrator.ExecuteTestAsync(config);

            // Generate reports
            var reportGenerator = new ReportGenerator();
            var outputDir = options.OutputDirectory ?? "./test-results";
            var jsonPath = Path.Combine(outputDir, $"{config.ScenarioName}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");

            await reportGenerator.GenerateJsonReportAsync(result, jsonPath);

            // Output to console for CI/CD
            Console.WriteLine($"Test completed: {result.Status}");
            Console.WriteLine($"Total requests: {result.Metrics.TotalRequests}");
            Console.WriteLine($"Error rate: {result.Metrics.ErrorRate:P2}");
            Console.WriteLine($"P95 response time: {result.Metrics.P95ResponseTime.TotalMilliseconds:F2}ms");
            Console.WriteLine($"Throughput: {result.Metrics.AverageThroughput:F2} req/s");
            Console.WriteLine($"Report saved to: {jsonPath}");

            // Return appropriate exit code
            return result.Status switch
            {
                TestStatus.Passed => ExitCodes.Success,
                TestStatus.Failed => ExitCodes.TestFailed,
                TestStatus.Error => ExitCodes.ExecutionError,
                _ => ExitCodes.ExecutionError
            };
        }
        catch (FileNotFoundException ex)
        {
            Console.Error.WriteLine($"Configuration file not found: {ex.Message}");
            return ExitCodes.ConfigurationError;
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"Configuration error: {ex.Message}");
            return ExitCodes.ConfigurationError;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"System error: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return ExitCodes.SystemError;
        }
    }

    private RunOptions? ParseArguments(string[] args)
    {
        if (args.Length == 0)
        {
            return null;
        }

        var options = new RunOptions();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--config" or "-c":
                    if (i + 1 < args.Length)
                    {
                        options.ConfigFilePath = args[++i];
                    }

                    break;

                case "--output" or "-o":
                    if (i + 1 < args.Length)
                    {
                        options.OutputDirectory = args[++i];
                    }

                    break;

                case "--base-url" or "-u":
                    if (i + 1 < args.Length)
                    {
                        options.BaseUrl = args[++i];
                    }

                    break;

                case "--help" or "-h":
                    return null;
            }
        }

        if (string.IsNullOrWhiteSpace(options.ConfigFilePath))
        {
            return null;
        }

        return options;
    }

    private void PrintUsage()
    {
        Console.WriteLine("Performance Test Runner");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  PerformanceTests --config <path> [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -c, --config <path>      Path to test configuration file (required)");
        Console.WriteLine("  -o, --output <path>      Output directory for reports (default: ./test-results)");
        Console.WriteLine("  -u, --base-url <url>     Base URL for the system under test (default: http://localhost:5000)");
        Console.WriteLine("  -h, --help               Show this help message");
        Console.WriteLine();
        Console.WriteLine("Exit Codes:");
        Console.WriteLine("  0 - All tests passed");
        Console.WriteLine("  1 - One or more tests failed");
        Console.WriteLine("  2 - Configuration error");
        Console.WriteLine("  3 - System error");
        Console.WriteLine("  4 - Execution error (>50% error rate)");
    }
}

internal class RunOptions
{
    public string ConfigFilePath { get; set; } = string.Empty;

    public string? OutputDirectory { get; set; }

    public string? BaseUrl { get; set; }
}
