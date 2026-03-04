using CompanyName.MyMeetings.PerformanceTests.Models;

namespace CompanyName.MyMeetings.PerformanceTests.Interfaces;

public interface ITestConfiguration
{
    string ScenarioName { get; }

    TestEndpoint[] Endpoints { get; }

    LoadParameters LoadParams { get; }

    TimeSpan Duration { get; }

    TimeSpan WarmupPeriod { get; }

    SuccessCriteria Criteria { get; }

    AuthenticationConfig? Authentication { get; }
}
