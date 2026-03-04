namespace CompanyName.MyMeetings.PerformanceTests.Configuration;

public class TestConfigurationDto
{
    public string? ScenarioName { get; set; }

    public List<EndpointDto>? Endpoints { get; set; }

    public LoadParametersDto? LoadParameters { get; set; }

    public string? Duration { get; set; }

    public string? WarmupPeriod { get; set; }

    public SuccessCriteriaDto? SuccessCriteria { get; set; }

    public AuthenticationDto? Authentication { get; set; }
}

public class EndpointDto
{
    public string? Url { get; set; }

    public string? Method { get; set; }

    public string? RequestBody { get; set; }

    public Dictionary<string, string>? Headers { get; set; }
}

public class LoadParametersDto
{
    public int VirtualUsers { get; set; }

    public RampUpStrategyDto? RampUpStrategy { get; set; }

    public ThinkTimeDto? ThinkTime { get; set; }
}

public class RampUpStrategyDto
{
    public string? Type { get; set; }

    public string? Duration { get; set; }

    public int? StepSize { get; set; }

    public string? StepDuration { get; set; }
}

public class ThinkTimeDto
{
    public string? MinDelay { get; set; }

    public string? MaxDelay { get; set; }
}

public class SuccessCriteriaDto
{
    public string? MaxResponseTime { get; set; }

    public double? MinThroughput { get; set; }

    public double? MaxErrorRate { get; set; }
}

public class AuthenticationDto
{
    public string? Type { get; set; }

    public string? TokenEndpoint { get; set; }

    public Dictionary<string, string>? Credentials { get; set; }
}
