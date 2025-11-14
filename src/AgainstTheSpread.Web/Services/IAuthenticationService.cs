namespace AgainstTheSpread.Web.Services;

/// <summary>
/// Service for handling user authentication
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Check if the user is authenticated
    /// </summary>
    Task<bool> IsAuthenticatedAsync();

    /// <summary>
    /// Get the current user's email
    /// </summary>
    Task<string?> GetUserEmailAsync();

    /// <summary>
    /// Get the full user info
    /// </summary>
    Task<UserInfo?> GetUserInfoAsync();
}

/// <summary>
/// User information from authentication
/// </summary>
public class UserInfo
{
    public string? Email { get; set; }
    public string? Name { get; set; }
    public string? UserId { get; set; }
    public bool IsAuthenticated { get; set; }
}
