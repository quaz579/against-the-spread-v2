using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace AgainstTheSpread.Functions.Helpers;

/// <summary>
/// Represents authenticated user information extracted from SWA authentication.
/// </summary>
/// <param name="UserId">The unique user identifier from the identity provider.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="DisplayName">The user's display name (optional).</param>
public record UserInfo(string UserId, string Email, string? DisplayName);

/// <summary>
/// Helper class for handling authentication and authorization in Azure Functions.
/// Extracts user information from Azure Static Web Apps authentication headers.
/// </summary>
public class AuthHelper
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of AuthHelper.
    /// </summary>
    /// <param name="logger">Logger for diagnostic information.</param>
    public AuthHelper(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates authentication from request headers and extracts user information.
    /// </summary>
    /// <param name="headers">Request headers dictionary.</param>
    /// <returns>
    /// A tuple containing:
    /// - IsAuthenticated: Whether the user is authenticated
    /// - User: The extracted user information (null if not authenticated)
    /// - Error: Error message if authentication failed (null if successful)
    /// </returns>
    public (bool IsAuthenticated, UserInfo? User, string? Error) ValidateAuth(
        IDictionary<string, IEnumerable<string>> headers)
    {
        // Check for test auth bypass (dev/test environments only)
        if (IsTestAuthEnabled() && headers.TryGetValue("X-Test-User-Email", out var testEmailValues))
        {
            var testEmail = testEmailValues.FirstOrDefault();
            if (!string.IsNullOrEmpty(testEmail))
            {
                _logger.LogInformation("Test auth bypass: authenticating as {Email}", testEmail);
                return (true, new UserInfo(
                    UserId: $"test-{testEmail.GetHashCode():X8}",
                    Email: testEmail,
                    DisplayName: testEmail.Split('@')[0]
                ), null);
            }
        }

        // Get the client principal from SWA auth header
        if (!headers.TryGetValue("X-MS-CLIENT-PRINCIPAL", out var principalValues))
        {
            _logger.LogWarning("No authentication header found");
            return (false, null, "Authentication required");
        }

        var principalHeader = principalValues.FirstOrDefault();
        if (string.IsNullOrEmpty(principalHeader))
        {
            _logger.LogWarning("Empty authentication header");
            return (false, null, "Authentication required");
        }

        try
        {
            // Decode the base64-encoded principal
            var principalJson = Encoding.UTF8.GetString(Convert.FromBase64String(principalHeader));
            var principal = JsonSerializer.Deserialize<ClientPrincipal>(principalJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (principal == null || string.IsNullOrEmpty(principal.UserId))
            {
                _logger.LogWarning("Invalid principal data");
                return (false, null, "Invalid authentication");
            }

            // Try to extract email from various locations
            string? userEmail = null;

            // 1. Check "email" claim type
            userEmail = principal.Claims
                ?.FirstOrDefault(c => c.Typ?.Equals("email", StringComparison.OrdinalIgnoreCase) == true)
                ?.Val;

            // 2. Check xmlsoap email claim type
            if (string.IsNullOrEmpty(userEmail))
            {
                userEmail = principal.Claims
                    ?.FirstOrDefault(c => c.Typ?.Equals("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", StringComparison.OrdinalIgnoreCase) == true)
                    ?.Val;
            }

            // 3. Check UserDetails property - Google OAuth often stores email here
            if (string.IsNullOrEmpty(userEmail) && IsValidEmail(principal.UserDetails))
            {
                userEmail = principal.UserDetails;
                _logger.LogInformation("Email retrieved from UserDetails for user {UserId}", principal.UserId);
            }

            if (string.IsNullOrEmpty(userEmail))
            {
                _logger.LogWarning("No email found in claims or UserDetails for user {UserId}. Claims: {Claims}",
                    principal.UserId,
                    principal.Claims?.Select(c => $"{c.Typ}={c.Val}").ToList() ?? new List<string>());
                return (false, null, "Email not found in authentication");
            }

            // Extract display name from claims
            var displayName = principal.Claims
                ?.FirstOrDefault(c => c.Typ?.Equals("name", StringComparison.OrdinalIgnoreCase) == true)
                ?.Val;

            var userInfo = new UserInfo(principal.UserId, userEmail, displayName);
            _logger.LogInformation("User authenticated: {Email}", userEmail);
            return (true, userInfo, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating authentication");
            return (false, null, "Authentication validation failed");
        }
    }

    /// <summary>
    /// Checks if the specified user is an admin.
    /// </summary>
    /// <param name="user">The user information to check.</param>
    /// <returns>True if the user is an admin, false otherwise.</returns>
    public bool IsAdmin(UserInfo user)
    {
        var adminEmailsConfig = Environment.GetEnvironmentVariable("ADMIN_EMAILS") ?? "";
        var adminEmails = adminEmailsConfig
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(e => e.ToLowerInvariant())
            .ToHashSet();

        if (adminEmails.Count == 0)
        {
            _logger.LogWarning("No admin emails configured");
            return false;
        }

        var isAdmin = adminEmails.Contains(user.Email.ToLowerInvariant());
        if (isAdmin)
        {
            _logger.LogInformation("Admin access verified for {Email}", user.Email);
        }
        else
        {
            _logger.LogWarning("User {Email} is not authorized as admin", user.Email);
        }

        return isAdmin;
    }

    /// <summary>
    /// Simple email validation check.
    /// </summary>
    private static bool IsValidEmail(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        // Simple check for email format
        return value.Contains('@') && value.Contains('.');
    }

    /// <summary>
    /// Checks if test auth bypass is enabled.
    /// Only enable this in dev/test environments, NEVER in production.
    /// </summary>
    private static bool IsTestAuthEnabled() =>
        Environment.GetEnvironmentVariable("ENABLE_TEST_AUTH")?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
}

/// <summary>
/// Represents the client principal from SWA authentication.
/// </summary>
internal class ClientPrincipal
{
    public string? IdentityProvider { get; set; }
    public string? UserId { get; set; }
    public string? UserDetails { get; set; }
    public List<string>? UserRoles { get; set; }
    public List<ClientPrincipalClaim>? Claims { get; set; }
}

/// <summary>
/// Represents a claim in the client principal.
/// </summary>
internal class ClientPrincipalClaim
{
    public string? Typ { get; set; }
    public string? Val { get; set; }
}
