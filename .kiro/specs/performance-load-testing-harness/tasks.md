# Implementation Plan: Performance Load Testing Harness

## Overview

This implementation plan breaks down the Performance Load Testing Harness into three phases as defined in the design document. Phase 1 establishes core load testing capabilities with essential metrics. Phase 2 adds advanced monitoring, visualization, and regression detection. Phase 3 enables multi-module and event-driven workflow testing.

Each task references specific requirements and includes property-based tests to validate correctness properties from the design document. Tasks marked with `*` are optional and can be skipped for faster delivery.

## Phase 1: Core Load Testing (MVP)

### 1. Project Setup and Core Infrastructure

- [x] 1.1 Create test project structure and configure dependencies
  - Create `MyMeetings.PerformanceTests` project in the solution
  - Add NuGet packages: xUnit, FsCheck, YamlDotNet, System.Text.Json
  - Set up project folders: Unit/, Properties/, Generators/, Integration/
  - Configure test project to reference MyMeetings.IntegrationTests for SUT initialization
  - _Requirements: 9.1, 9.3, 9.4_

- [x] 1.2 Create core interfaces and data models
  - Implement ITestConfiguration, ILoadGenerator, IMetricsCollector interfaces
  - Create data transfer objects: TestEndpoint, LoadParameters, RampUpStrategy, SuccessCriteria
  - Create metric models: RequestMetric, MetricsSummary, TestResult
  - _Requirements: 4.2, 4.3, 4.4, 4.5_

- [ ]* 1.3 Write property test for configuration round-trip
  - **Property 18: Configuration Deserialization Round-Trip**
  - **Validates: Requirements 4.1**

### 2. Configuration Loading and Validation

- [x] 2.1 Implement configuration parser
  - Create ConfigurationLoader class to parse YAML/JSON test configurations
  - Implement deserialization using YamlDotNet and System.Text.Json
  - Support loading from file paths
  - _Requirements: 4.1_

- [x] 2.2 Implement configuration validation
  - Create ConfigurationValidator class
  - Validate required fields: endpoints, virtual users, duration, success criteria
  - Validate value ranges: positive durations, valid URLs, non-negative user counts
  - Return validation errors with clear messages
  - _Requirements: 4.7_

- [ ]* 2.3 Write property tests for configuration validation
  - **Property 19: Configuration Completeness**
  - **Property 21: Configuration Validation**
  - **Validates: Requirements 4.2, 4.3, 4.4, 4.5, 4.7, 10.5**

### 3. Metrics Collection Infrastructure

- [x] 3.1 Implement MetricsCollector class
  - Create thread-safe metrics storage using ConcurrentDictionary and ConcurrentBag
  - Implement RecordRequest method to capture RequestMetric objects
  - Store response times, status codes, timestamps, endpoint URLs
  - Track warmup vs. actual test metrics separately
  - _Requirements: 2.1, 2.5, 2.7_

- [x] 3.2 Implement percentile calculation
  - Create method to calculate p50, p95, p99 from response time collection
  - Sort response times and extract percentile values
  - Handle edge cases: empty collections, single value
  - _Requirements: 2.2_

- [x] 3.3 Implement throughput and error rate calculation
  - Calculate throughput as total requests / test duration
  - Calculate error rate as (failed requests / total requests) * 100
  - Track status code distribution in ConcurrentDictionary
  - _Requirements: 2.3, 2.4_

- [x] 3.4 Implement time-series bucketing
  - Bucket metrics into 1-second intervals using epoch seconds
  - Aggregate request counts, error counts, and response times per bucket
  - Store in ConcurrentDictionary<long, SecondBucket>
  - _Requirements: 2.6_

- [ ]* 3.5 Write property tests for metrics collection
  - **Property 7: Request Metric Recording**
  - **Property 8: Percentile Calculation Accuracy**
  - **Property 9: Throughput Calculation**
  - **Property 10: Error Rate and Status Code Tracking**
  - **Property 11: Metric Timestamp Precision**
  - **Property 12: Time-Series Bucketing**
  - **Property 13: Warmup Period Separation**
  - **Validates: Requirements 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 11.1, 11.2**

### 4. Load Generator Implementation

- [x] 4.1 Implement VirtualUser class
  - Create VirtualUser class that executes requests in a loop
  - Use HttpClient to send requests to configured endpoints
  - Record metrics for each request (timestamp, response time, status code)
  - Handle exceptions and record error metrics
  - Support cancellation via CancellationToken
  - _Requirements: 1.1, 1.2_

- [x] 4.2 Implement LoadGenerator with immediate ramp-up
  - Create LoadGenerator class implementing ILoadGenerator
  - Implement immediate ramp-up strategy (all users start simultaneously)
  - Create N virtual user tasks and start them concurrently
  - Coordinate test phases: warmup, steady-state, completion
  - _Requirements: 1.2, 1.3, 10.3_

- [x] 4.3 Implement test duration and timing control
  - Track test start time and enforce configured duration
  - Implement warmup period tracking and metric flagging
  - Cancel all virtual user tasks when duration expires
  - _Requirements: 1.5, 2.7_

- [ ]* 4.4 Write property tests for load generation
  - **Property 1: HTTP Request Execution**
  - **Property 2: Virtual User Creation**
  - **Property 5: Test Duration Accuracy**
  - **Validates: Requirements 1.1, 1.2, 1.5**

### 5. Test Orchestration

- [x] 5.1 Implement TestOrchestrator class
  - Create TestOrchestrator to coordinate test execution lifecycle
  - Load and validate test configuration
  - Initialize SUT using IntegrationTestWebAppFactory pattern
  - Create and start LoadGenerator with MetricsCollector
  - Determine pass/fail status based on success criteria
  - _Requirements: 1.6, 9.1_

- [x] 5.2 Implement success criteria evaluation
  - Compare metrics against configured success criteria
  - Check max response time (p95 or p99)
  - Check min throughput
  - Check max error rate (5% threshold)
  - Set test status to Passed or Failed
  - _Requirements: 4.5, 11.3_

- [ ]* 5.3 Write property test for success criteria evaluation
  - **Property 38: Success Criteria Evaluation**
  - **Validates: Requirements 8.6**

### 6. JSON Report Generation

- [x] 6.1 Implement JSON report generator
  - Create ReportGenerator class implementing IReportGenerator
  - Implement GenerateJsonReportAsync method
  - Serialize TestResult to JSON using System.Text.Json
  - Include all metrics: summary, time-series, percentiles, status codes
  - Write JSON to specified output path
  - _Requirements: 1.6, 8.1_

- [x] 6.2 Include error details in reports
  - Add error collection to TestResult model
  - Capture error details: timestamp, endpoint, status code, message
  - Include errors in JSON output
  - _Requirements: 11.4_

- [ ]* 6.3 Write property test for report generation
  - **Property 6: Report Generation Completeness**
  - **Validates: Requirements 1.6, 8.1, 8.2, 8.3, 8.4, 8.5, 8.6**

### 7. CI/CD Integration

- [x] 7.1 Implement exit code handling
  - Return exit code 0 when all tests pass
  - Return exit code 1 when tests fail success criteria
  - Return exit code 2 for configuration errors
  - Return exit code 3 for SUT startup failures
  - Return exit code 4 for execution errors (>50% error rate)
  - _Requirements: 13.1, 13.2_

- [x] 7.2 Implement command-line interface
  - Create CLI entry point for test execution
  - Support command-line parameters: config file path, output directory
  - Support running subset of test scenarios via parameters
  - Output JSON to stdout for CI/CD tool consumption
  - _Requirements: 13.3, 13.4_

- [ ]* 7.3 Write property tests for exit codes
  - **Property 46: Exit Code for Passed Tests**
  - **Property 47: Exit Code for Failed Tests**
  - **Property 48: CI/CD Output Format**
  - **Property 49: Test Subset Execution**
  - **Validates: Requirements 13.1, 13.2, 13.3, 13.4**

### 8. Error Handling and Resilience

- [x] 8.1 Implement error handling in VirtualUser
  - Catch and log HTTP request exceptions
  - Record error metrics with appropriate status codes
  - Continue execution unless cancellation requested
  - _Requirements: 11.1, 11.5_

- [x] 8.2 Implement high error rate termination
  - Monitor error rate during test execution
  - Terminate test early if error rate exceeds 50%
  - Set exit code 4 for execution errors
  - _Requirements: 11.5_

- [ ]* 8.3 Write property test for error rate handling
  - **Property 40: Error Rate Test Failure**
  - **Property 41: High Error Rate Test Termination**
  - **Validates: Requirements 11.3, 11.5**

### 9. Phase 1 Integration and Testing

- [x] 9.1 Create end-to-end integration test
  - Write integration test that executes a complete load test
  - Use test configuration with 10 virtual users, 30 second duration
  - Verify metrics are collected correctly
  - Verify JSON report is generated
  - Verify exit codes are correct
  - _Requirements: 1.1, 1.2, 1.5, 1.6_

- [x] 9.2 Create sample test configurations
  - Create sample YAML configurations for common scenarios
  - Include examples for each module: Administration, Meetings, Payments, Registrations, UserAccess
  - Document configuration options
  - _Requirements: 4.1, 6.1_

- [x] 9.3 Checkpoint - Ensure all Phase 1 tests pass
  - Run all unit tests and property tests
  - Run integration tests
  - Verify all Phase 1 requirements are met
  - Ask the user if questions arise

## Phase 2: Advanced Metrics & Reporting

### 10. Resource Monitoring

- [x] 10.1 Implement ResourceMonitor class
  - Create ResourceMonitor implementing IResourceMonitor
  - Track CPU utilization using .NET Performance Counters
  - Track memory consumption in megabytes
  - Track database connection counts per module using EF connection pool metrics
  - _Requirements: 3.1, 3.2, 3.3_

- [x] 10.2 Implement resource sampling
  - Sample resources every 5 seconds in background task
  - Store ResourceSample objects with timestamps
  - Correlate samples with concurrent load levels
  - Continue test execution if monitoring fails
  - _Requirements: 3.4, 3.6_

- [x] 10.3 Implement resource threshold flagging
  - Define configurable thresholds for CPU, memory, DB connections
  - Flag threshold violations in test results
  - Include flagged violations in test report
  - _Requirements: 3.5_

- [ ]* 10.4 Write property tests for resource monitoring
  - **Property 14: Resource Monitoring Completeness**
  - **Property 15: Resource Sampling Interval**
  - **Property 16: Resource Threshold Flagging**
  - **Property 17: Resource-Load Correlation**
  - **Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5, 3.6**

### 11. HTML Report Generation with Visualizations

- [x] 11.1 Implement HTML report template
  - Create HTML template with embedded CSS
  - Include sections: summary, time-series graphs, percentiles, resource utilization
  - Use Chart.js for client-side graph rendering
  - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5_

- [x] 11.2 Implement time-series graph generation
  - Generate JSON data for Chart.js from time-series buckets
  - Create graphs: throughput over time, response time over time, error rate over time
  - Create resource utilization graphs: CPU, memory, DB connections
  - _Requirements: 8.3, 8.5_

- [x] 11.3 Implement GenerateHtmlReportAsync method
  - Populate HTML template with test results
  - Embed time-series data as JSON for graphs
  - Write HTML to specified output path
  - Handle graph generation failures gracefully
  - _Requirements: 8.1_

### 12. Baseline Repository and Comparison

- [x] 12.1 Implement BaselineRepository class
  - Create BaselineRepository implementing IBaselineRepository
  - Store baselines as JSON files in test project directory
  - Implement GetBaselineAsync to load baseline for scenario
  - Implement SaveBaselineAsync to persist baseline metrics
  - _Requirements: 5.1_

- [x] 12.2 Implement baseline comparison logic
  - Create CompareAsync method to compare current metrics with baseline
  - Calculate percentage differences for p95 response time and throughput
  - Flag regression if p95 exceeds baseline by >20%
  - Flag regression if throughput falls below baseline by >15%
  - _Requirements: 5.2, 5.3, 5.4_

- [x] 12.3 Integrate baseline comparison into reports
  - Include baseline comparison results in TestResult model
  - Display regression analysis in HTML and JSON reports
  - Show side-by-side comparison of current vs. baseline metrics
  - _Requirements: 5.5, 8.7_

- [ ]* 12.4 Write property tests for baseline comparison
  - **Property 22: Baseline Persistence Round-Trip**
  - **Property 23: Baseline Comparison Execution**
  - **Property 24: Response Time Regression Detection**
  - **Property 25: Throughput Regression Detection**
  - **Property 26: Baseline Update Capability**
  - **Validates: Requirements 5.1, 5.2, 5.3, 5.4, 5.5, 5.6**

### 13. Advanced Ramp-Up Strategies

- [x] 13.1 Implement linear ramp-up strategy
  - Calculate user creation interval: rampUpDuration / virtualUsers
  - Start virtual users at regular intervals
  - Track actual vs. expected user creation times
  - _Requirements: 10.1_

- [x] 13.2 Implement step ramp-up strategy
  - Create users in discrete steps (e.g., 10 users every 30 seconds)
  - Support configurable step size and step duration
  - Maintain steady-state after final step
  - _Requirements: 10.2_

- [x] 13.3 Refactor LoadGenerator to support all strategies
  - Update LoadGenerator to accept RampUpStrategy configuration
  - Implement strategy pattern for ramp-up execution
  - Ensure steady-state load is maintained after ramp-up completes
  - _Requirements: 1.3, 1.4, 10.4_

- [ ]* 13.4 Write property tests for ramp-up strategies
  - **Property 3: Ramp-Up Strategy Adherence**
  - **Property 4: Steady-State Load Maintenance**
  - **Validates: Requirements 1.3, 1.4, 10.1, 10.2, 10.3, 10.4**

### 14. Think Time Simulation

- [x] 14.1 Implement think time in VirtualUser
  - Add think time delay between requests
  - Randomize delay within configured min/max range
  - Use Task.Delay for non-blocking wait
  - _Requirements: 12.1, 12.3_

- [x] 14.2 Ensure think time exclusion from response time
  - Measure response time only for HTTP request/response duration
  - Do not include think time delay in recorded metrics
  - _Requirements: 12.4_

- [x] 14.3 Support disabling think time
  - Allow configuration with no think time for maximum throughput tests
  - Virtual users execute requests continuously when disabled
  - _Requirements: 12.5_

- [ ]* 14.4 Write property tests for think time
  - **Property 42: Think Time Application**
  - **Property 43: Think Time Exclusion from Response Time**
  - **Property 44: Think Time Configuration**
  - **Property 45: Think Time Disablement**
  - **Validates: Requirements 12.1, 12.2, 12.3, 12.4, 12.5**

### 15. Phase 2 Integration and Testing

- [x] 15.1 Create integration tests for Phase 2 features
  - Test resource monitoring during load test
  - Test HTML report generation with graphs
  - Test baseline comparison with known baselines
  - Test linear and step ramp-up strategies
  - Test think time simulation
  - _Requirements: 3.1, 5.2, 8.1, 10.1, 12.1_

- [x] 15.2 Checkpoint - Ensure all Phase 2 tests pass
  - Run all unit tests and property tests
  - Run integration tests
  - Verify all Phase 2 requirements are met
  - Ask the user if questions arise

## Phase 3: Multi-Module & Event-Driven Testing

### 16. Multi-Module Test Support

- [x] 16.1 Implement per-module metrics tracking
  - Extend MetricsCollector to track metrics by module
  - Parse module name from endpoint URL
  - Store separate metric summaries per module
  - _Requirements: 6.3_

- [x] 16.2 Implement per-module database connection tracking
  - Extend ResourceMonitor to track DB connections per module database
  - Query connection pool metrics for each module's database
  - Store connection counts separately by module
  - _Requirements: 6.4_

- [x] 16.3 Update reports with per-module breakdowns
  - Add module-level metrics to TestResult model
  - Display per-module metrics in HTML and JSON reports
  - Show request counts, response times, error rates per module
  - _Requirements: 6.3_

- [ ]* 16.4 Write property tests for multi-module support
  - **Property 27: Multi-Module Endpoint Support**
  - **Property 28: Module Specification in Configuration**
  - **Property 29: Per-Module Metrics Reporting**
  - **Property 30: Per-Module Database Connection Tracking**
  - **Validates: Requirements 6.1, 6.2, 6.3, 6.4**

### 17. Cross-Module Workflow Support

- [x] 17.1 Implement workflow sequence configuration
  - Extend test configuration to support workflow sequences
  - Define ordered list of operations across modules
  - Support passing data between workflow steps (e.g., meeting ID from creation to registration)
  - _Requirements: 6.5_

- [x] 17.2 Implement workflow execution in VirtualUser
  - Execute workflow steps in sequence
  - Extract and pass identifiers between steps
  - Record metrics for each step in the workflow
  - _Requirements: 6.5_

- [ ]* 17.3 Write property test for workflow sequences
  - **Property 31: Cross-Module Workflow Sequence Definition**
  - **Validates: Requirements 6.5**

### 18. Event-Driven Workflow Testing

- [x] 18.1 Implement event processing latency tracking
  - Hook into Outbox and Inbox processing to capture timestamps
  - Measure time from command execution to outbox write
  - Measure time from outbox write to inbox processing
  - Measure time from inbox processing to final event completion
  - _Requirements: 7.1, 7.2, 7.3_

- [x] 18.2 Implement end-to-end latency measurement
  - Track complete workflow from initial command to final event
  - Store event processing metrics in MetricsCollector
  - Calculate end-to-end latency for each workflow execution
  - _Requirements: 7.1_

- [x] 18.3 Implement event processing verification
  - Poll for event completion within expected timeframe
  - Flag events that exceed 5 second processing time
  - Include delayed events in test report with details
  - _Requirements: 7.4, 7.5_

- [ ]* 18.4 Write property tests for event-driven workflows
  - **Property 32: End-to-End Event Latency Measurement**
  - **Property 33: Outbox Processing Latency Tracking**
  - **Property 34: Inbox Processing Latency Tracking**
  - **Property 35: Event Processing Timeframe Verification**
  - **Property 36: Event Processing Delay Flagging**
  - **Validates: Requirements 7.1, 7.2, 7.3, 7.4, 7.5**

### 19. Data Generation and Test Data Management

- [x] 19.1 Implement parameterized payload generation
  - Create PayloadGenerator class
  - Support variable placeholders: {{randomString}}, {{futureDate}}, {{randomInt:min:max}}
  - Replace placeholders with generated values for each request
  - _Requirements: 14.1_

- [x] 19.2 Implement unique identifier generation
  - Generate unique GUIDs for each request
  - Ensure uniqueness across all virtual users
  - Support sequential ID generation as alternative
  - _Requirements: 14.2_

- [x] 19.3 Implement setup and cleanup scripts
  - Support executing setup scripts before test execution
  - Support executing cleanup scripts after test completion
  - Ensure cleanup runs even if test fails
  - _Requirements: 14.3, 14.4_

- [x] 19.4 Implement data generation strategies
  - Support sequential data generation (incrementing IDs)
  - Support random data generation (random strings, dates, numbers)
  - Support loading data from files (CSV, JSON)
  - Configure strategy in test configuration
  - _Requirements: 14.5_

- [ ]* 19.5 Write property tests for data generation
  - **Property 51: Parameterized Payload Variable Replacement**
  - **Property 52: Unique Identifier Generation**
  - **Property 53: Setup Script Execution Order**
  - **Property 54: Cleanup Script Execution Order**
  - **Property 55: Data Generation Strategy Specification**
  - **Validates: Requirements 14.1, 14.2, 14.3, 14.4, 14.5**

### 20. Authentication Integration

- [x] 20.1 Implement authentication token management
  - Reuse authentication mechanisms from integration tests
  - Support Bearer token authentication
  - Implement token acquisition from configured endpoint
  - Cache and reuse tokens across virtual users
  - _Requirements: 4.6, 9.5_

- [x] 20.2 Implement authentication configuration
  - Extend test configuration to include authentication settings
  - Support username/password credentials
  - Support pre-generated tokens
  - _Requirements: 4.6_

- [ ]* 20.3 Write property test for authentication configuration
  - **Property 20: Authentication Configuration Presence**
  - **Validates: Requirements 4.6**

### 21. Phase 3 Integration and Testing

- [x] 21.1 Create end-to-end multi-module workflow tests
  - Test cross-module workflow: Meeting creation → Registration → Payment
  - Verify per-module metrics are tracked correctly
  - Verify event-driven workflow latency is measured
  - _Requirements: 6.1, 6.5, 7.1_

- [x] 21.2 Create event-driven workflow integration tests
  - Test Outbox/Inbox latency tracking
  - Test event processing delay flagging
  - Verify end-to-end latency measurements
  - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_

- [x] 21.3 Verify CI/CD execution time constraint
  - Run full test suite and measure execution time
  - Ensure total execution time is under 15 minutes
  - Optimize if necessary
  - _Requirements: 13.5_

- [ ]* 21.4 Write property test for CI/CD execution time
  - **Property 50: CI/CD Execution Time Limit**
  - **Validates: Requirements 13.5**

- [x] 21.5 Final checkpoint - Ensure all tests pass
  - Run complete test suite (all phases)
  - Verify all 55 correctness properties pass
  - Verify all requirements are met
  - Generate comprehensive test report
  - Ask the user if questions arise

## Notes

- Tasks marked with `*` are optional property-based tests that can be skipped for faster MVP delivery
- Each phase builds on the previous phase - complete Phase 1 before starting Phase 2
- Property tests should run with minimum 100 iterations as specified in the design
- All code should integrate with existing MyMeetings test infrastructure patterns
- Maintain backward compatibility between phases (Phase 1 configs should work in Phase 3)
- Each checkpoint task ensures incremental validation before proceeding to the next phase
