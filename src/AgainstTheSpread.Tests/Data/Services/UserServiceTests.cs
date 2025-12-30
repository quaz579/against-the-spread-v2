using AgainstTheSpread.Data;
using AgainstTheSpread.Data.Interfaces;
using AgainstTheSpread.Data.Entities;
using AgainstTheSpread.Data.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace AgainstTheSpread.Tests.Data.Services;

/// <summary>
/// Tests for UserService implementation.
/// Uses InMemory database provider for testing.
/// </summary>
public class UserServiceTests : IDisposable
{
    private readonly AtsDbContext _context;
    private readonly IUserService _userService;
    private readonly Mock<ILogger<UserService>> _mockLogger;

    public UserServiceTests()
    {
        var options = new DbContextOptionsBuilder<AtsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AtsDbContext(options);
        _mockLogger = new Mock<ILogger<UserService>>();
        _userService = new UserService(_context, _mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region GetByGoogleSubjectIdAsync Tests

    [Fact]
    public async Task GetByGoogleSubjectIdAsync_WithExistingUser_ReturnsUser()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            GoogleSubjectId = "google-existing-123",
            Email = "existing@example.com",
            DisplayName = "Existing User",
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
            IsActive = true
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.GetByGoogleSubjectIdAsync("google-existing-123");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Email.Should().Be("existing@example.com");
        result.DisplayName.Should().Be("Existing User");
    }

    [Fact]
    public async Task GetByGoogleSubjectIdAsync_WithNonExistingUser_ReturnsNull()
    {
        // Arrange - empty database

        // Act
        var result = await _userService.GetByGoogleSubjectIdAsync("non-existing-id");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByGoogleSubjectIdAsync_WithInactiveUser_ReturnsUser()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            GoogleSubjectId = "google-inactive-123",
            Email = "inactive@example.com",
            DisplayName = "Inactive User",
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
            IsActive = false
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.GetByGoogleSubjectIdAsync("google-inactive-123");

        // Assert - should still return user even if inactive
        result.Should().NotBeNull();
        result!.IsActive.Should().BeFalse();
    }

    #endregion

    #region GetOrCreateUserAsync Tests

    [Fact]
    public async Task GetOrCreateUserAsync_WithExistingUser_ReturnsExistingAndUpdatesLastLogin()
    {
        // Arrange
        var originalLoginTime = DateTime.UtcNow.AddDays(-1);
        var user = new User
        {
            Id = Guid.NewGuid(),
            GoogleSubjectId = "google-getorcreate-123",
            Email = "getorcreate@example.com",
            DisplayName = "Original Name",
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            LastLoginAt = originalLoginTime,
            IsActive = true
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.GetOrCreateUserAsync(
            "google-getorcreate-123",
            "newemail@example.com", // Different email should be ignored
            "New Display Name"); // Different name should be ignored

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(user.Id);
        result.Email.Should().Be("getorcreate@example.com"); // Original email preserved
        result.DisplayName.Should().Be("Original Name"); // Original name preserved
        result.LastLoginAt.Should().BeAfter(originalLoginTime); // Login time updated
    }

    [Fact]
    public async Task GetOrCreateUserAsync_WithNewUser_CreatesUser()
    {
        // Arrange - empty database
        var beforeCreate = DateTime.UtcNow;

        // Act
        var result = await _userService.GetOrCreateUserAsync(
            "google-new-user-456",
            "newuser@example.com",
            "New User");

        // Assert
        result.Should().NotBeNull();
        result.GoogleSubjectId.Should().Be("google-new-user-456");
        result.Email.Should().Be("newuser@example.com");
        result.DisplayName.Should().Be("New User");
        result.CreatedAt.Should().BeOnOrAfter(beforeCreate);
        result.LastLoginAt.Should().BeOnOrAfter(beforeCreate);
        result.IsActive.Should().BeTrue();

        // Verify persisted
        var persisted = await _context.Users.FindAsync(result.Id);
        persisted.Should().NotBeNull();
    }

    [Fact]
    public async Task GetOrCreateUserAsync_WithNullDisplayName_CreatesUserWithEmailAsDisplayName()
    {
        // Arrange - empty database

        // Act
        var result = await _userService.GetOrCreateUserAsync(
            "google-null-display-789",
            "nulldisplay@example.com",
            null!);

        // Assert
        result.Should().NotBeNull();
        result.DisplayName.Should().Be("nulldisplay@example.com");
    }

    [Fact]
    public async Task GetOrCreateUserAsync_WithEmptyDisplayName_CreatesUserWithEmailAsDisplayName()
    {
        // Arrange - empty database

        // Act
        var result = await _userService.GetOrCreateUserAsync(
            "google-empty-display-789",
            "emptydisplay@example.com",
            "");

        // Assert
        result.Should().NotBeNull();
        result.DisplayName.Should().Be("emptydisplay@example.com");
    }

    #endregion

    #region UpdateLastLoginAsync Tests

    [Fact]
    public async Task UpdateLastLoginAsync_WithExistingUser_UpdatesTimestamp()
    {
        // Arrange
        var originalLoginTime = DateTime.UtcNow.AddDays(-7);
        var user = new User
        {
            Id = Guid.NewGuid(),
            GoogleSubjectId = "google-update-login-123",
            Email = "updatelogin@example.com",
            DisplayName = "Update Login User",
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            LastLoginAt = originalLoginTime,
            IsActive = true
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.UpdateLastLoginAsync(user.Id);

        // Assert
        result.Should().BeTrue();

        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser!.LastLoginAt.Should().BeAfter(originalLoginTime);
    }

    [Fact]
    public async Task UpdateLastLoginAsync_WithNonExistingUser_ReturnsFalse()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _userService.UpdateLastLoginAsync(nonExistingId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingUser_ReturnsUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            GoogleSubjectId = "google-byid-123",
            Email = "byid@example.com",
            DisplayName = "By ID User",
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
            IsActive = true
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.GetByIdAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(userId);
        result.Email.Should().Be("byid@example.com");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingUser_ReturnsNull()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _userService.GetByIdAsync(nonExistingId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Concurrent Access Tests

    [Fact]
    public async Task GetOrCreateUserAsync_ConcurrentCreation_DoesNotCreateDuplicates()
    {
        // Arrange
        var googleSubjectId = "google-concurrent-123";
        var email = "concurrent@example.com";
        var displayName = "Concurrent User";

        // Act - Simulate concurrent calls
        var tasks = Enumerable.Range(0, 5).Select(_ =>
            _userService.GetOrCreateUserAsync(googleSubjectId, email, displayName));

        var results = await Task.WhenAll(tasks);

        // Assert - All should return the same user
        var distinctUserIds = results.Select(r => r.Id).Distinct().ToList();
        distinctUserIds.Should().HaveCount(1);

        // Verify only one user in database
        var usersInDb = await _context.Users
            .Where(u => u.GoogleSubjectId == googleSubjectId)
            .ToListAsync();
        usersInDb.Should().HaveCount(1);
    }

    #endregion
}
