using CompanyName.MyMeetings.PerformanceTests.Authentication;
using CompanyName.MyMeetings.PerformanceTests.Interfaces;
using CompanyName.MyMeetings.PerformanceTests.Models;

namespace CompanyName.MyMeetings.PerformanceTests.LoadGeneration;

public class VirtualUser
{
    private readonly HttpClient _httpClient;
    private readonly ITestConfiguration _config;
    private readonly IMetricsCollector _metrics;
    private readonly IAuthenticationProvider _authProvider;
    private readonly Random _random;
    private readonly DateTime _testStartTime;
    private readonly TimeSpan _warmupPeriod;

    public VirtualUser(
        HttpClient httpClient,
        ITestConfiguration config,
        IMetricsCollector metrics,
        IAuthenticationProvider authProvider,
        DateTime testStartTime)
    {
        _httpClient = httpClient;
        _config = config;
        _metrics = metrics;
        _authProvider = authProvider;
        _random = new Random();
        _testStartTime = testStartTime;
        _warmupPeriod = config.WarmupPeriod;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var endpoint = SelectEndpoint();
            var startTime = DateTime.UtcNow;
            var isWarmup = IsWarmupPhase(startTime);

            try
            {
                var request = CreateRequest(endpoint);
                var response = await _httpClient.SendAsync(request, cancellationToken);

                var responseTime = DateTime.UtcNow - startTime;

                _metrics.RecordRequest(new RequestMetric(
                    startTime,
                    responseTime,
                    (int)response.StatusCode,
                    endpoint.Url,
                    isWarmup));
            }
            catch (HttpRequestException)
            {
                var responseTime = DateTime.UtcNow - startTime;
                _metrics.RecordRequest(new RequestMetric(
                    startTime,
                    responseTime,
                    0, // Status code 0 indicates network error
                    endpoint.Url,
                    isWarmup));
            }
            catch (TaskCanceledException)
            {
                // Test duration expired, exit gracefully
                break;
            }
            catch (Exception)
            {
                var responseTime = DateTime.UtcNow - startTime;
                _metrics.RecordRequest(new RequestMetric(
                    startTime,
                    responseTime,
                    500, // Generic error status
                    endpoint.Url,
                    isWarmup));
            }

            await ApplyThinkTimeAsync(cancellationToken);
        }
    }

    private TestEndpoint SelectEndpoint()
    {
        if (_config.Endpoints.Length == 1)
        {
            return _config.Endpoints[0];
        }

        var index = _random.Next(_config.Endpoints.Length);
        return _config.Endpoints[index];
    }

    private HttpRequestMessage CreateRequest(TestEndpoint endpoint)
    {
        var request = new HttpRequestMessage(endpoint.Method, endpoint.Url);

        if (!string.IsNullOrWhiteSpace(endpoint.RequestBody))
        {
            request.Content = new StringContent(
                endpoint.RequestBody,
                System.Text.Encoding.UTF8,
                "application/json");
        }

        if (endpoint.Headers != null)
        {
            foreach (var header in endpoint.Headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        // Add authentication token if available
        var token = _authProvider.GetTokenAsync().GetAwaiter().GetResult();
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        return request;
    }

    private bool IsWarmupPhase(DateTime currentTime)
    {
        var elapsed = currentTime - _testStartTime;
        return elapsed < _warmupPeriod;
    }

    private async Task ApplyThinkTimeAsync(CancellationToken cancellationToken)
    {
        if (_config.LoadParams.ThinkTime == null)
        {
            return;
        }

        var minMs = (int)_config.LoadParams.ThinkTime.MinDelay.TotalMilliseconds;
        var maxMs = (int)_config.LoadParams.ThinkTime.MaxDelay.TotalMilliseconds;
        var delayMs = _random.Next(minMs, maxMs + 1);

        try
        {
            await Task.Delay(delayMs, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            // Test duration expired, exit gracefully
        }
    }
}
