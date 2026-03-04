namespace CompanyName.MyMeetings.PerformanceTests.Models;

public enum RampUpType
{
    Linear,
    Step,
    Immediate
}

public record RampUpStrategy(
    RampUpType Type,
    int? StepSize = null,
    TimeSpan? StepDuration = null);
