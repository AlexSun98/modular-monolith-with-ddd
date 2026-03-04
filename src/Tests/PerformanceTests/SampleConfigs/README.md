# Sample Performance Test Configurations

This directory contains sample YAML configuration files for performance testing the MyMeetings application.

## Configuration Files

### simple-load-test.yaml
A basic load test configuration that tests GET requests to the meetings endpoint with 10 virtual users.

**Usage:**
```bash
dotnet test --filter "FullyQualifiedName~PerformanceTests" -- --config SampleConfigs/simple-load-test.yaml
```

### meeting-group-list-load-test.yaml
Tests the meeting groups listing endpoint with 20 virtual users. This endpoint requires authentication.

**Note:** This test will fail with authentication errors unless bearer token authentication is configured. The MyMeetings API requires valid authentication tokens for all endpoints.

**Usage:**
```bash
dotnet run --project src/Tests/PerformanceTests -- \
  --config SampleConfigs/meeting-group-list-load-test.yaml \
  --output ./test-results \
  --base-url http://localhost:5000
```

**To run successfully, you need to:**
1. Configure bearer token authentication in the test configuration
2. Provide valid credentials for token acquisition
3. Ensure the MyMeetings API is running with the database initialized

## Configuration Options

### Required Fields
- `scenarioName`: Name of the test scenario
- `endpoints`: Array of endpoints to test
  - `url`: Endpoint URL (relative or absolute)
  - `method`: HTTP method (GET, POST, PUT, DELETE, PATCH)
  - `requestBody`: (optional) Request body for POST/PUT/PATCH
  - `headers`: (optional) HTTP headers
- `loadParameters`: Load generation settings
  - `virtualUsers`: Number of concurrent virtual users
  - `rampUpStrategy`: How users are created
    - `type`: immediate, linear, or step
    - `duration`: Time to ramp up (for linear/step)
- `duration`: Total test duration (format: HH:MM:SS)
- `warmupPeriod`: Warmup period before collecting metrics
- `successCriteria`: Pass/fail criteria
  - `maxResponseTime`: Maximum acceptable response time
  - `minThroughput`: Minimum requests per second
  - `maxErrorRate`: Maximum error rate (0.0 to 1.0)

### Optional Fields
- `authentication`: Authentication configuration
  - `type`: Authentication type (Bearer, Basic, etc.)
  - `tokenEndpoint`: Endpoint to obtain auth token
  - `credentials`: Username/password or other credentials

## Phase 1 Limitations

In Phase 1, the following features were implemented:
- ✅ Immediate ramp-up strategy
- ✅ Basic HTTP methods (GET, POST, PUT, DELETE, PATCH)
- ✅ JSON report generation
- ✅ Success criteria evaluation
- ✅ High error rate termination

Phase 2 added:
- ✅ Linear and step ramp-up strategies
- ✅ Think time simulation
- ✅ HTML reports with interactive charts
- ✅ Resource monitoring (CPU, memory)
- ✅ Baseline comparison and regression detection

Phase 3 features (require application integration):
- 🚧 Multi-module workflow testing
- 🚧 Event-driven workflow testing
- 🚧 Per-module database connection tracking
- 🚧 Parameterized payload generation
- 🚧 Full authentication integration

## Authentication Requirements

All MyMeetings API endpoints require authentication via bearer tokens. The harness includes bearer token support (`BearerTokenProvider`), but it needs to be configured with:

1. Token endpoint URL (IdentityServer4)
2. Client credentials (client ID, client secret)
3. User credentials or client credentials flow

Without authentication configured, tests against the MyMeetings API will fail with 500 status codes ("User context is not available").

## Running Tests Against MyMeetings API

The sample configurations demonstrate the harness capabilities but require proper setup:

1. **Start the API**: Ensure the MyMeetings API is running at http://localhost:5000
2. **Initialize Database**: Run database migrations to create the schema
3. **Configure Authentication**: Set up bearer token acquisition (currently not configured)
4. **Create Test Data**: For POST endpoints, ensure required foreign keys exist (e.g., meetingGroupId)

The harness successfully detects authentication errors and measures metrics even when tests fail, demonstrating its error detection and reporting capabilities.
