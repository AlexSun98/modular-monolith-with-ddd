using System.Text.Json;
using CompanyName.MyMeetings.PerformanceTests.Models;

namespace CompanyName.MyMeetings.PerformanceTests.Authentication;

public class BearerTokenProvider : IAuthenticationProvider
{
    private readonly HttpClient _httpClient;
    private readonly AuthenticationConfig _authConfig;
    private string? _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public BearerTokenProvider(HttpClient httpClient, AuthenticationConfig authConfig)
    {
        _httpClient = httpClient;
        _authConfig = authConfig;
    }

    public async Task<string?> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        // Return cached token if still valid
        if (_cachedToken != null && DateTime.UtcNow < _tokenExpiry)
        {
            return _cachedToken;
        }

        // Acquire new token
        try
        {
            var tokenEndpoint = _authConfig.TokenEndpoint ?? "/api/auth/token";
            var credentials = _authConfig.Credentials ?? new Dictionary<string, string>();

            // Use form-urlencoded for OAuth2/IdentityServer4 token requests
            var content = new FormUrlEncodedContent(credentials);

            var response = await _httpClient.PostAsync(tokenEndpoint, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Try different property names for token
            var token = tokenResponse?.AccessToken ?? tokenResponse?.Access_Token ?? tokenResponse?.Token;

            if (token != null)
            {
                _cachedToken = token;

                // Use expires_in from response if available, otherwise default to 50 minutes
                var expiresInSeconds = tokenResponse?.ExpiresIn ?? 3000;
                _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresInSeconds - 600); // 10 min buffer

                return _cachedToken;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private class TokenResponse
    {
        public string? Token { get; set; }

        public string? AccessToken { get; set; }

        public string? Access_Token { get; set; }

        public int? ExpiresIn { get; set; }

        public int? Expires_In { get; set; }
    }
}
