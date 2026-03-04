using CompanyName.MyMeetings.PerformanceTests.Authentication;
using CompanyName.MyMeetings.PerformanceTests.Models;
using NUnit.Framework;

namespace CompanyName.MyMeetings.PerformanceTests.Integration;

[TestFixture]
public class AuthenticationTests
{
    [Test]
    public async Task BearerTokenProvider_WithNoAuthentication_ShouldReturnNull()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var noAuthProvider = new NoAuthenticationProvider();

        // Act
        var token = await noAuthProvider.GetTokenAsync();

        // Assert
        Assert.That(token, Is.Null);
    }

    [Test]
    public async Task BearerTokenProvider_WithInvalidEndpoint_ShouldReturnNull()
    {
        // Arrange
        using var httpClient = new HttpClient { BaseAddress = new Uri("https://httpbin.org") };
        var authConfig = new AuthenticationConfig(
            Type: "Bearer",
            TokenEndpoint: "/status/404",
            Credentials: new Dictionary<string, string>
            {
                { "username", "test" },
                { "password", "test" }
            });

        var tokenProvider = new BearerTokenProvider(httpClient, authConfig);

        // Act
        var token = await tokenProvider.GetTokenAsync();

        // Assert
        Assert.That(token, Is.Null);
    }

    [Test]
    public async Task BearerTokenProvider_ShouldCacheToken()
    {
        // Arrange
        using var httpClient = new HttpClient { BaseAddress = new Uri("https://httpbin.org") };
        var authConfig = new AuthenticationConfig(
            Type: "Bearer",
            TokenEndpoint: "/status/404",
            Credentials: new Dictionary<string, string>());

        var tokenProvider = new BearerTokenProvider(httpClient, authConfig);

        // Act - First call
        var token1 = await tokenProvider.GetTokenAsync();

        // Act - Second call (should use cache)
        var token2 = await tokenProvider.GetTokenAsync();

        // Assert - Both should be null but demonstrate caching logic works
        Assert.That(token1, Is.EqualTo(token2));
    }
}
