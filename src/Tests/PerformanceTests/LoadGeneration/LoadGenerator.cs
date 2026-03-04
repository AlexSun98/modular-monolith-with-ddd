using CompanyName.MyMeetings.PerformanceTests.Authentication;
using CompanyName.MyMeetings.PerformanceTests.Interfaces;
using CompanyName.MyMeetings.PerformanceTests.Models;

namespace CompanyName.MyMeetings.PerformanceTests.LoadGeneration;

public class LoadGenerator : ILoadGenerator
{
    private readonly HttpClient _httpClient;

    public LoadGenerator(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<LoadTestResult> ExecuteAsync(
        ITestConfiguration config,
        IMetricsCollector metrics,
        CancellationToken cancellationToken)
    {
        var testStartTime = DateTime.UtcNow;
        var testDuration = config.Duration;
        var virtualUserCount = config.LoadParams.VirtualUsers;

        // Create authentication provider
        var authProvider = CreateAuthenticationProvider(config);

        // Pre-acquire token if authentication is configured
        if (config.Authentication != null)
        {
            await authProvider.GetTokenAsync(cancellationToken);
        }

        // Create cancellation token source for test duration
        using var testCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        testCts.CancelAfter(testDuration);

        // Create virtual users
        var virtualUsers = new List<VirtualUser>();
        for (int i = 0; i < virtualUserCount; i++)
        {
            virtualUsers.Add(new VirtualUser(_httpClient, config, metrics, authProvider, testStartTime));
        }

        // Execute ramp-up strategy
        var tasks = await ExecuteRampUpAsync(virtualUsers, config.LoadParams.Strategy, testCts.Token);

        // Monitor error rate and terminate if it exceeds 50%
        var monitoringTask = Task.Run(
            async () =>
            {
                while (!testCts.Token.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), testCts.Token);

                    var summary = metrics.GetSummary();
                    if (summary.TotalRequests > 10 && summary.ErrorRate > 0.5)
                    {
                        // Error rate exceeds 50%, terminate test
                        testCts.Cancel();
                        break;
                    }
                }
            },
            testCts.Token);

        // Wait for all virtual users to complete
        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            // Test duration expired or high error rate, this is expected
        }

        // Collect results
        var summary = metrics.GetSummary();
        return new LoadTestResult
        {
            TotalRequests = summary.TotalRequests,
            SuccessfulRequests = summary.SuccessfulRequests,
            FailedRequests = summary.FailedRequests,
            Metrics = new List<RequestMetric>() // Metrics are already in the collector
        };
    }

    private IAuthenticationProvider CreateAuthenticationProvider(ITestConfiguration config)
    {
        if (config.Authentication == null)
        {
            return new NoAuthenticationProvider();
        }

        if (config.Authentication.Type.Equals("Bearer", StringComparison.OrdinalIgnoreCase))
        {
            return new BearerTokenProvider(_httpClient, config.Authentication);
        }

        return new NoAuthenticationProvider();
    }

    private async Task<List<Task>> ExecuteRampUpAsync(
        List<VirtualUser> virtualUsers,
        RampUpStrategy strategy,
        CancellationToken cancellationToken)
    {
        var tasks = new List<Task>();

        switch (strategy.Type)
        {
            case RampUpType.Immediate:
                // Start all virtual users immediately
                foreach (var user in virtualUsers)
                {
                    tasks.Add(Task.Run(() => user.ExecuteAsync(cancellationToken), cancellationToken));
                }

                break;

            case RampUpType.Linear:
                // Linear ramp-up: start users at regular intervals
                var totalUsers = virtualUsers.Count;
                var rampUpDuration = strategy.StepDuration ?? TimeSpan.FromSeconds(30);
                var intervalMs = rampUpDuration.TotalMilliseconds / totalUsers;

                foreach (var user in virtualUsers)
                {
                    tasks.Add(Task.Run(() => user.ExecuteAsync(cancellationToken), cancellationToken));

                    if (intervalMs > 0)
                    {
                        try
                        {
                            await Task.Delay(TimeSpan.FromMilliseconds(intervalMs), cancellationToken);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                    }
                }

                break;

            case RampUpType.Step:
                // Step ramp-up: start users in discrete steps
                var stepSize = strategy.StepSize ?? 10;
                var stepDuration = strategy.StepDuration ?? TimeSpan.FromSeconds(10);
                var userIndex = 0;

                while (userIndex < virtualUsers.Count)
                {
                    var usersInStep = Math.Min(stepSize, virtualUsers.Count - userIndex);

                    for (int i = 0; i < usersInStep; i++)
                    {
                        var user = virtualUsers[userIndex++];
                        tasks.Add(Task.Run(() => user.ExecuteAsync(cancellationToken), cancellationToken));
                    }

                    if (userIndex < virtualUsers.Count)
                    {
                        try
                        {
                            await Task.Delay(stepDuration, cancellationToken);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                    }
                }

                break;

            default:
                throw new ArgumentException($"Unknown ramp-up strategy: {strategy.Type}");
        }

        return tasks;
    }
}
