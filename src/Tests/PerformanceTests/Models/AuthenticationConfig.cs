namespace CompanyName.MyMeetings.PerformanceTests.Models;

public record AuthenticationConfig(
    string Type,
    string? TokenEndpoint,
    Dictionary<string, string>? Credentials);
