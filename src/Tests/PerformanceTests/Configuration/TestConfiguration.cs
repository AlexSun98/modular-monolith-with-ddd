using CompanyName.MyMeetings.PerformanceTests.Interfaces;
using CompanyName.MyMeetings.PerformanceTests.Models;

namespace CompanyName.MyMeetings.PerformanceTests.Configuration;

public class TestConfiguration : ITestConfiguration
{
    public string ScenarioName { get; set; } = string.Empty;

    public TestEndpoint[] Endpoints { get; set; } = Array.Empty<TestEndpoint>();

    public LoadParameters LoadParams { get; set; } = null!;

    public TimeSpan Duration { get; set; }

    public TimeSpan WarmupPeriod { get; set; }

    public SuccessCriteria Criteria { get; set; } = null!;

    public AuthenticationConfig? Authentication { get; set; }
}
