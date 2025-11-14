using System.Text.Json;

namespace AgainstTheSpread.Web.Services;

/// <summary>
/// Service for handling user authentication via Azure Static Web Apps
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly HttpClient _httpClient;
    private UserInfo? _cachedUserInfo;

    public AuthenticationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
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
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking auth: {ex.Message}");
        }

        // Return non-authenticated user info
        return new UserInfo { IsAuthenticated = false };
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
