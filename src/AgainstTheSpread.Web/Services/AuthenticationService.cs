using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace AgainstTheSpread.Web.Services;

/// <summary>
/// Service for handling user authentication via Azure Static Web Apps
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthenticationService> _logger;
    private UserInfo? _cachedUserInfo;

    public AuthenticationService(HttpClient httpClient, ILogger<AuthenticationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var userInfo = await GetUserInfoAsync();
        return userInfo?.IsAuthenticated ?? false;
    }

    public async Task<string?> GetUserEmailAsync()
    {
        var userInfo = await GetUserInfoAsync();
        return userInfo?.Email;
    }

    public async Task<UserInfo?> GetUserInfoAsync()
    {
        // Return cached info if available
        if (_cachedUserInfo != null)
        {
            return _cachedUserInfo;
        }

        try
        {
            var response = await _httpClient.GetAsync("/.auth/me");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var authInfo = JsonSerializer.Deserialize<AuthMeResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (authInfo?.ClientPrincipal != null)
                {
                    _cachedUserInfo = new UserInfo
                    {
                        Email = authInfo.ClientPrincipal.UserDetails,
                        UserId = authInfo.ClientPrincipal.UserId,
                        Name = authInfo.ClientPrincipal.UserDetails?.Split('@')[0], // Use email prefix as name
                        IsAuthenticated = true
                    };
                    return _cachedUserInfo;
                }
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request error checking authentication status");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error checking authentication status");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error checking authentication status");
        }

        // Return non-authenticated user info
        return new UserInfo { IsAuthenticated = false };
    }

    public void ClearCache()
    {
        _cachedUserInfo = null;
    }

    // Helper classes for SWA auth
    private class AuthMeResponse
    {
        public ClientPrincipal? ClientPrincipal { get; set; }
    }

    private class ClientPrincipal
    {
        public string? IdentityProvider { get; set; }
        public string? UserId { get; set; }
        public string? UserDetails { get; set; }
        public List<string>? UserRoles { get; set; }
    }
}
