# Performance Load Testing Harness

A comprehensive performance and load testing framework for the MyMeetings modular monolith application.

## Overview

This harness enables systematic performance testing of REST API endpoints, CQRS command/query handlers, and event-driven workflows across all modules (Administration, Meetings, Payments, Registrations, UserAccess).

## Features Implemented

### Phase 1: Core Load Testing (MVP) ✅
- ✅ Test configuration loading from YAML/JSON files
- ✅ Configuration validation with detailed error messages
- ✅ Concurrent load generation with configurable virtual users
- ✅ Immediate ramp-up strategy
- ✅ Metrics collection (response times, throughput, error rates)
- ✅ Percentile calculations (p50, p95, p99)
- ✅ Time-series bucketing (1-second intervals)
- ✅ Warmup period support
- ✅ JSON report generation
- ✅ Success criteria evaluation
- ✅ CI/CD integration with exit codes
- ✅ High error rate termination (>50%)
- ✅ Command-line interface

### Phase 2: Advanced Metrics & Reporting ✅
- ✅ Resource monitoring (CPU, memory, database connections)
- ✅ Resource threshold flagging
- ✅ HTML report generation with embedded charts
- ✅ Time-series graphs (throughput, response time, error rate)
- ✅ Baseline repository for regression detection
- ✅ Baseline comparison (20% response time, 15% throughput thresholds)
- ✅ Linear ramp-up strategy
- ✅ Step ramp-up strategy
- ✅ Think time simulation
- ✅ Think time exclusion from response time measurements

### Phase 3: Multi-Module & Event-Driven Testing 🚧
Phase 3 features require deep integration with the MyMeetings application and are marked as placeholders:
- 🚧 Per-module metrics tracking (placeholder)
- 🚧 Per-module database connection tracking (placeholder)
- 🚧 Cross-module workflow sequences (requires application integration)
- 🚧 Event-driven workflow testing (requires Outbox/Inbox access)
- 🚧 Parameterized payload generation (requires implementation)
- 🚧 Authentication token management (requires auth endpoint)

## Project Structure

```
PerformanceTests/
├── Baseline/              # Baseline storage and comparison
├── CLI/                   # Command-line interface
├── Configuration/         # Configuration loading and validation
├── Generators/            # Property-based test generators (for future PBT tests)
├── Integration/           # Integration tests
├── Interfaces/            # Core interfaces
├── LoadGeneration/        # Load generator and virtual users
├── Metrics/               # Metrics collection and aggregation
├── Models/                # Data models and DTOs
├── Monitoring/            # Resource monitoring
├── Orchestration/         # Test orchestration
├── Properties/            # Property-based tests (for future implementation)
├── Reporting/             # Report generation (JSON and HTML)
├── SampleConfigs/         # Sample test configurations
└── Unit/                  # Unit tests (for future implementation)
```

## Usage

### Running Tests from Command Line

```bash
# Run a test with a configuration file
dotnet run --project src/Tests/PerformanceTests -- \
  --config SampleConfigs/simple-load-test.yaml \
  --output ./test-results \
  --base-url http://localhost:5000
```

### Running Tests Programmatically

```csharp
using var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };
var orchestrator = new TestOrchestrator(httpClient);

var config = new TestConfiguration
{
    ScenarioName = "My Load Test",
    Endpoints = new[] { new TestEndpoint("/api/meetings", HttpMethod.Get, null, null) },
    LoadParams = new LoadParameters(
        VirtualUsers: 10,
        Strategy: new RampUpStrategy(RampUpType.Linear),
        RampUpDuration: TimeSpan.FromSeconds(30),
        ThinkTime: new ThinkTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3))),
    Duration = TimeSpan.FromMinutes(5),
    WarmupPeriod = TimeSpan.FromSeconds(30),
    Criteria = new SuccessCriteria(
        MaxResponseTime: TimeSpan.FromSeconds(2),
        MinThroughput: 10.0,
        MaxErrorRate: 0.05)
};

var result = await orchestrator.ExecuteTestAsync(config);
```

### Configuration File Format

See `SampleConfigs/README.md` for detailed configuration documentation.

Example YAML configuration:

```yaml
scenarioName: "Simple Load Test"

endpoints:
  - url: "/api/meetings"
    method: GET
    headers:
      Accept: "application/json"

loadParameters:
  virtualUsers: 10
  rampUpStrategy:
    type: linear  # immediate, linear, or step
    duration: "00:00:30"
  thinkTime:
    minDelay: "00:00:01"
    maxDelay: "00:00:03"

duration: "00:05:00"
warmupPeriod: "00:00:30"

successCriteria:
  maxResponseTime: "00:00:02"
  minThroughput: 5.0
  maxErrorRate: 0.05
```

## Exit Codes

- **0**: All tests passed
- **1**: One or more tests failed (did not meet success criteria)
- **2**: Configuration error (invalid config, missing files)
- **3**: System error (SUT failed to start, critical infrastructure failure)
- **4**: Execution error (>50% error rate, >25% virtual user crashes)

## Reports

### JSON Reports
JSON reports are generated automatically and include:
- Test summary (requests, throughput, error rate)
- Percentile metrics (p50, p95, p99)
- Status code distribution
- Error details
- Baseline comparison (if enabled)

### HTML Reports
HTML reports include:
- Visual summary with metric cards
- Interactive time-series charts (Chart.js)
- Throughput over time
- Response time over time
- Error rate over time
- Error details section

## Baseline Management

### Saving a Baseline

```csharp
var baselineRepo = new BaselineRepository();
var baseline = new BaselineMetrics(
    ScenarioName: "My Test",
    CapturedAt: DateTime.UtcNow,
    P50ResponseTime: result.Metrics.P50ResponseTime,
    P95ResponseTime: result.Metrics.P95ResponseTime,
    P99ResponseTime: result.Metrics.P99ResponseTime,
    AverageThroughput: result.Metrics.AverageThroughput,
    ErrorRate: result.Metrics.ErrorRate);

await baselineRepo.SaveBaselineAsync("My Test", baseline);
```

### Comparing Against Baseline

```csharp
var comparison = await baselineRepo.CompareAsync("My Test", result.Metrics);

if (comparison.HasRegression)
{
    foreach (var flag in comparison.RegressionFlags)
    {
        Console.WriteLine($"Regression: {flag}");
    }
}
```

## Resource Monitoring

Resource monitoring tracks:
- CPU utilization percentage
- Memory consumption in MB
- Database connections per module (placeholder)

Samples are taken every 5 seconds and violations are flagged when thresholds are exceeded.

## Ramp-Up Strategies

### Immediate
All virtual users start simultaneously.

```yaml
rampUpStrategy:
  type: immediate
```

### Linear
Virtual users are created at regular intervals over the ramp-up duration.

```yaml
rampUpStrategy:
  type: linear
  duration: "00:00:30"  # 30 seconds to ramp up
```

### Step
Virtual users are created in discrete steps.

```yaml
rampUpStrategy:
  type: step
  stepSize: 10           # 10 users per step
  stepDuration: "00:00:10"  # 10 seconds between steps
```

## Think Time

Think time simulates realistic user behavior by adding delays between requests.

```yaml
thinkTime:
  minDelay: "00:00:01"  # 1 second minimum
  maxDelay: "00:00:03"  # 3 seconds maximum
```

Think time is excluded from response time measurements.

## Authentication

The harness includes bearer token authentication support via the `BearerTokenProvider` class. To run authenticated load tests against the MyMeetings API:

1. **Configure Authentication Endpoint**: Update the `BearerTokenProvider` to point to your IdentityServer4 token endpoint
2. **Provide Credentials**: Set up test user credentials for token acquisition
3. **Enable in Configuration**: Add authentication configuration to your YAML test files

Example authenticated configuration:

```yaml
scenarioName: "Authenticated API Test"

authentication:
  type: bearer
  tokenEndpoint: "http://localhost:5000/connect/token"
  clientId: "test-client"
  clientSecret: "test-secret"
  scope: "api"

endpoints:
  - url: "/api/meetings/meetingGroups"
    method: GET
    headers:
      Accept: "application/json"
```

## Known Limitations

1. **Phase 3 Features**: Multi-module testing, event-driven workflows, and advanced data generation require integration with the actual MyMeetings application.

2. **Database Connection Tracking**: Currently returns placeholder values. Requires access to Entity Framework connection pool metrics.

3. **Authentication Setup**: Bearer token support is implemented but requires configuration of the actual token endpoint and credentials for your environment.

4. **Test Data Dependencies**: Some endpoints (e.g., POST `/api/meetings/meetings`) require valid foreign keys (meetingGroupId) that must exist in the database before testing.

5. **Property-Based Tests**: Optional property-based tests (marked with `*` in tasks) were skipped for faster delivery. These can be added later for comprehensive validation.

6. **Build Issues**: The project uses NUnit 3.13.3 for FsCheck compatibility, which may conflict with other test projects using NUnit 4.x.

## Next Steps

To complete Phase 3:

1. **Integrate with SUT**: Connect to the actual MyMeetings application using IntegrationTestWebAppFactory
2. **Implement Module Detection**: Parse endpoint URLs to determine which module is being tested
3. **Add Event Tracking**: Hook into Outbox/Inbox processing to measure event latency
4. **Implement Payload Generation**: Add variable replacement for {{randomString}}, {{futureDate}}, etc.
5. **Add Authentication**: Implement token acquisition from the auth endpoint
6. **Write Property-Based Tests**: Add FsCheck property tests for comprehensive validation

## Contributing

When adding new features:
1. Follow the existing project structure
2. Add appropriate interfaces in the `Interfaces/` folder
3. Implement concrete classes in feature-specific folders
4. Update models in `Models/` as needed
5. Add integration tests in `Integration/`
6. Update this README with new features

## License

This project is part of the MyMeetings modular monolith application.
