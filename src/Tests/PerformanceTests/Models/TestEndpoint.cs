namespace CompanyName.MyMeetings.PerformanceTests.Models;

public record TestEndpoint(
    string Url,
    HttpMethod Method,
    string? RequestBody,
    Dictionary<string, string>? Headers);
