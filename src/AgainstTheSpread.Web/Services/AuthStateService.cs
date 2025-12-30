using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AgainstTheSpread.Web.Services;

/// <summary>
/// Service for managing user authentication state client-side.
/// Uses Azure Static Web Apps authentication via /.auth/me endpoint.
/// </summary>
public interface IAuthStateService
{
    /// <summary>
    /// Initialize the authentication state by checking the /.auth/me endpoint.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Whether the user is currently authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// The user's unique ID from the identity provider.
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// The user's email address.
    /// </summary>
    string? UserEmail { get; }

    /// <summary>
    /// The user's display name (usually same as email for Google auth).
    /// </summary>
    string? UserName { get; }

    /// <summary>
    /// The identity provider (e.g., "google").
    /// </summary>
    string? IdentityProvider { get; }

    /// <summary>
    /// Navigate to the login page.
    /// </summary>
    void Login(string? returnUrl = null);

    /// <summary>
    /// Navigate to the logout endpoint.
    /// </summary>
    void Logout(string? returnUrl = null);
}

public class AuthStateService : IAuthStateService
{
    private readonly NavigationManager _navigationManager;
    private readonly ILogger<AuthStateService> _logger;
    private readonly HttpClient _httpClient;

    private bool _isInitialized;
    private bool _isAuthenticated;
    private string? _userId;
    private string? _userEmail;
    private string? _userName;
    private string? _identityProvider;

    public AuthStateService(
        NavigationManager navigationManager,
        ILogger<AuthStateService> logger,
        HttpClient httpClient)
    {
        _navigationManager = navigationManager;
        _logger = logger;
        _httpClient = httpClient;
    }

    public bool IsAuthenticated => _isAuthenticated;
    public string? UserId => _userId;
    public string? UserEmail => _userEmail;
    public string? UserName => _userName;
    public string? IdentityProvider => _identityProvider;

    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        try
        {
            // Create a client with the base address for auth endpoints
            using var authClient = new HttpClient { BaseAddress = new Uri(_navigationManager.BaseUri) };

            var response = await authClient.GetAsync(".auth/me");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("Auth response: {AuthResponse}", json);

                var authInfo = JsonSerializer.Deserialize<AuthMeResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (authInfo?.ClientPrincipal != null)
                {
                    _isAuthenticated = true;
                    _userId = authInfo.ClientPrincipal.UserId;
                    _userEmail = authInfo.ClientPrincipal.UserDetails;
                    _userName = authInfo.ClientPrincipal.UserDetails; // SWA uses UserDetails for display name
                    _identityProvider = authInfo.ClientPrincipal.IdentityProvider;

                    _logger.LogInformation("User authenticated: {Email}", _userEmail);
                }
            }
            else
            {
                _logger.LogWarning("Auth check failed with status code: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking authentication status");
        }
        finally
        {
            _isInitialized = true;
        }
    }

    public void Login(string? returnUrl = null)
    {
        var redirectUri = returnUrl ?? _navigationManager.Uri;
        // Ensure absolute URL for redirect
        if (!redirectUri.StartsWith("http"))
        {
            redirectUri = new Uri(new Uri(_navigationManager.BaseUri), redirectUri).ToString();
        }

        _navigationManager.NavigateTo(
            $"/.auth/login/google?post_login_redirect_uri={Uri.EscapeDataString(redirectUri)}",
            forceLoad: true);
    }

    public void Logout(string? returnUrl = null)
    {
        var redirectUri = returnUrl ?? "/";
        // Ensure absolute URL for redirect
        if (!redirectUri.StartsWith("http"))
        {
            redirectUri = new Uri(new Uri(_navigationManager.BaseUri), redirectUri).ToString();
        }

        _navigationManager.NavigateTo(
            $"/.auth/logout?post_logout_redirect_uri={Uri.EscapeDataString(redirectUri)}",
            forceLoad: true);
    }

    // Helper classes for SWA auth response
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
