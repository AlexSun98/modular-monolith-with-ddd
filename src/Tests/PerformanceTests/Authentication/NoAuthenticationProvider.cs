namespace CompanyName.MyMeetings.PerformanceTests.Authentication;

public class NoAuthenticationProvider : IAuthenticationProvider
{
    public Task<string?> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<string?>(null);
    }
}
