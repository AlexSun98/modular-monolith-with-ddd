namespace CompanyName.MyMeetings.PerformanceTests.Authentication;

public interface IAuthenticationProvider
{
    Task<string?> GetTokenAsync(CancellationToken cancellationToken = default);
}
