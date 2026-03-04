namespace CompanyName.MyMeetings.PerformanceTests.Models;

public record LoadParameters(
    int VirtualUsers,
    RampUpStrategy Strategy,
    TimeSpan RampUpDuration,
    ThinkTime? ThinkTime);
