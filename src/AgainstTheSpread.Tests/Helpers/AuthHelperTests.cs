using AgainstTheSpread.Functions.Helpers;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using System.Text.Json;

namespace AgainstTheSpread.Tests.Helpers;

/// <summary>
/// Tests for AuthHelper authentication and authorization logic.
/// Validates extraction of user info from SWA authentication headers.
/// </summary>
public class AuthHelperTests
{
    private readonly Mock<ILogger> _mockLogger;
    private readonly AuthHelper _authHelper;

    public AuthHelperTests()
    {
        _mockLogger = new Mock<ILogger>();
        _authHelper = new AuthHelper(_mockLogger.Object);
    }

    #region UserInfo Record Tests

    [Fact]
    public void UserInfo_WithAllProperties_CreatesCorrectRecord()
    {
        // Arrange & Act
        var userInfo = new UserInfo("user-123", "test@example.com", "Test User");

        // Assert
        userInfo.UserId.Should().Be("user-123");
        userInfo.Email.Should().Be("test@example.com");
        userInfo.DisplayName.Should().Be("Test User");
    }

    [Fact]
    public void UserInfo_WithNullDisplayName_CreatesCorrectRecord()
    {
        // Arrange & Act
        var userInfo = new UserInfo("user-123", "test@example.com", null);

        // Assert
        userInfo.UserId.Should().Be("user-123");
        userInfo.Email.Should().Be("test@example.com");
        userInfo.DisplayName.Should().BeNull();
    }

    [Fact]
    public void UserInfo_Equality_WorksCorrectly()
    {
        // Arrange
        var userInfo1 = new UserInfo("user-123", "test@example.com", "Test User");
        var userInfo2 = new UserInfo("user-123", "test@example.com", "Test User");

        // Assert
        userInfo1.Should().Be(userInfo2);
    }

    #endregion

    #region ValidateAuth Tests

    [Fact]
    public void ValidateAuth_WithNoPrincipalHeader_ReturnsNotAuthenticated()
    {
        // Arrange
        var headers = new Dictionary<string, IEnumerable<string>>();

        // Act
        var result = _authHelper.ValidateAuth(headers);

        // Assert
        result.IsAuthenticated.Should().BeFalse();
        result.User.Should().BeNull();
        result.Error.Should().Be("Authentication required");
    }

    [Fact]
    public void ValidateAuth_WithEmptyPrincipalHeader_ReturnsNotAuthenticated()
    {
        // Arrange
        var headers = new Dictionary<string, IEnumerable<string>>
        {
            { "X-MS-CLIENT-PRINCIPAL", new[] { "" } }
        };

        // Act
        var result = _authHelper.ValidateAuth(headers);

        // Assert
        result.IsAuthenticated.Should().BeFalse();
        result.User.Should().BeNull();
        result.Error.Should().Be("Authentication required");
    }

    [Fact]
    public void ValidateAuth_WithInvalidBase64_ReturnsNotAuthenticated()
    {
        // Arrange
        var headers = new Dictionary<string, IEnumerable<string>>
        {
            { "X-MS-CLIENT-PRINCIPAL", new[] { "not-valid-base64!!!" } }
        };

        // Act
        var result = _authHelper.ValidateAuth(headers);

        // Assert
        result.IsAuthenticated.Should().BeFalse();
        result.User.Should().BeNull();
        result.Error.Should().Be("Authentication validation failed");
    }

    [Fact]
    public void ValidateAuth_WithValidPrincipal_EmailInClaims_ReturnsAuthenticated()
    {
        // Arrange
        var principal = new
        {
            identityProvider = "google",
            userId = "google-123",
            userDetails = "testuser",
            userRoles = new[] { "authenticated" },
            claims = new[]
            {
                new { typ = "email", val = "test@example.com" },
                new { typ = "name", val = "Test User" }
            }
        };
        var principalJson = JsonSerializer.Serialize(principal);
        var principalBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(principalJson));

        var headers = new Dictionary<string, IEnumerable<string>>
        {
            { "X-MS-CLIENT-PRINCIPAL", new[] { principalBase64 } }
        };

        // Act
        var result = _authHelper.ValidateAuth(headers);

        // Assert
        result.IsAuthenticated.Should().BeTrue();
        result.User.Should().NotBeNull();
        result.User!.UserId.Should().Be("google-123");
        result.User.Email.Should().Be("test@example.com");
        result.Error.Should().BeNull();
    }

    [Fact]
    public void ValidateAuth_WithValidPrincipal_EmailInUserDetails_ReturnsAuthenticated()
    {
        // Arrange - Google OAuth often puts email in UserDetails
        var principal = new
        {
            identityProvider = "google",
            userId = "google-456",
            userDetails = "user@gmail.com",
            userRoles = new[] { "authenticated" },
            claims = new[]
            {
                new { typ = "name", val = "Gmail User" }
            }
        };
        var principalJson = JsonSerializer.Serialize(principal);
        var principalBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(principalJson));

        var headers = new Dictionary<string, IEnumerable<string>>
        {
            { "X-MS-CLIENT-PRINCIPAL", new[] { principalBase64 } }
        };

        // Act
        var result = _authHelper.ValidateAuth(headers);

        // Assert
        result.IsAuthenticated.Should().BeTrue();
        result.User.Should().NotBeNull();
        result.User!.UserId.Should().Be("google-456");
        result.User.Email.Should().Be("user@gmail.com");
        result.Error.Should().BeNull();
    }

    [Fact]
    public void ValidateAuth_WithNoEmailAnywhere_ReturnsNotAuthenticated()
    {
        // Arrange
        var principal = new
        {
            identityProvider = "google",
            userId = "google-789",
            userDetails = "some-non-email-value",
            userRoles = new[] { "authenticated" },
            claims = new[]
            {
                new { typ = "name", val = "No Email User" }
            }
        };
        var principalJson = JsonSerializer.Serialize(principal);
        var principalBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(principalJson));

        var headers = new Dictionary<string, IEnumerable<string>>
        {
            { "X-MS-CLIENT-PRINCIPAL", new[] { principalBase64 } }
        };

        // Act
        var result = _authHelper.ValidateAuth(headers);

        // Assert
        result.IsAuthenticated.Should().BeFalse();
        result.User.Should().BeNull();
        result.Error.Should().Be("Email not found in authentication");
    }

    [Fact]
    public void ValidateAuth_WithAlternativeEmailClaimType_ReturnsAuthenticated()
    {
        // Arrange - Sometimes email comes via xmlsoap claim type
        var principal = new
        {
            identityProvider = "google",
            userId = "google-alt",
            userDetails = "username",
            userRoles = new[] { "authenticated" },
            claims = new[]
            {
                new { typ = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", val = "alt@example.com" },
                new { typ = "name", val = "Alt User" }
            }
        };
        var principalJson = JsonSerializer.Serialize(principal);
        var principalBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(principalJson));

        var headers = new Dictionary<string, IEnumerable<string>>
        {
            { "X-MS-CLIENT-PRINCIPAL", new[] { principalBase64 } }
        };

        // Act
        var result = _authHelper.ValidateAuth(headers);

        // Assert
        result.IsAuthenticated.Should().BeTrue();
        result.User.Should().NotBeNull();
        result.User!.Email.Should().Be("alt@example.com");
        result.Error.Should().BeNull();
    }

    [Fact]
    public void ValidateAuth_ExtractsDisplayName_FromNameClaim()
    {
        // Arrange
        var principal = new
        {
            identityProvider = "google",
            userId = "google-name",
            userDetails = "named@example.com",
            userRoles = new[] { "authenticated" },
            claims = new[]
            {
                new { typ = "name", val = "Display Name" }
            }
        };
        var principalJson = JsonSerializer.Serialize(principal);
        var principalBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(principalJson));

        var headers = new Dictionary<string, IEnumerable<string>>
        {
            { "X-MS-CLIENT-PRINCIPAL", new[] { principalBase64 } }
        };

        // Act
        var result = _authHelper.ValidateAuth(headers);

        // Assert
        result.IsAuthenticated.Should().BeTrue();
        result.User!.DisplayName.Should().Be("Display Name");
    }

    #endregion

    #region IsAdmin Tests

    [Fact]
    public void IsAdmin_WithAdminEmail_ReturnsTrue()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ADMIN_EMAILS", "admin@example.com,other@example.com");
        var user = new UserInfo("user-1", "admin@example.com", "Admin");

        // Act
        var result = _authHelper.IsAdmin(user);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAdmin_WithNonAdminEmail_ReturnsFalse()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ADMIN_EMAILS", "admin@example.com,other@example.com");
        var user = new UserInfo("user-2", "regular@example.com", "Regular User");

        // Act
        var result = _authHelper.IsAdmin(user);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAdmin_CaseInsensitive_ReturnsTrue()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ADMIN_EMAILS", "Admin@Example.COM");
        var user = new UserInfo("user-3", "admin@example.com", "Admin");

        // Act
        var result = _authHelper.IsAdmin(user);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAdmin_WithNoAdminEmailsConfigured_ReturnsFalse()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ADMIN_EMAILS", "");
        var user = new UserInfo("user-4", "admin@example.com", "Admin");

        // Act
        var result = _authHelper.IsAdmin(user);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAdmin_WithWhitespaceInEmailList_HandlesCorrectly()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ADMIN_EMAILS", " admin@example.com , other@example.com ");
        var user = new UserInfo("user-5", "admin@example.com", "Admin");

        // Act
        var result = _authHelper.IsAdmin(user);

        // Assert
        result.Should().BeTrue();
    }

    #endregion
}
