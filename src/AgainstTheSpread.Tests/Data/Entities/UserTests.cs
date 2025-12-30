using AgainstTheSpread.Data.Entities;
using FluentAssertions;

namespace AgainstTheSpread.Tests.Data.Entities;

public class UserTests
{
    [Fact]
    public void User_NewInstance_HasEmptyGuid()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        user.Id.Should().Be(Guid.Empty);
    }

    [Fact]
    public void User_GoogleSubjectId_DefaultsToEmptyString()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        user.GoogleSubjectId.Should().BeEmpty();
    }

    [Fact]
    public void User_Email_DefaultsToEmptyString()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        user.Email.Should().BeEmpty();
    }

    [Fact]
    public void User_DisplayName_DefaultsToEmptyString()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        user.DisplayName.Should().BeEmpty();
    }

    [Fact]
    public void User_IsActive_DefaultsToTrue()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void User_Picks_DefaultsToEmptyCollection()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        user.Picks.Should().NotBeNull();
        user.Picks.Should().BeEmpty();
    }

    [Fact]
    public void User_CreatedAt_DefaultsToMinValue()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        user.CreatedAt.Should().Be(default(DateTime));
    }

    [Fact]
    public void User_LastLoginAt_DefaultsToMinValue()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        user.LastLoginAt.Should().Be(default(DateTime));
    }

    [Fact]
    public void User_CanSetAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var googleSubjectId = "google-123456";
        var email = "test@example.com";
        var displayName = "Test User";
        var createdAt = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var lastLoginAt = new DateTime(2024, 6, 15, 14, 30, 0, DateTimeKind.Utc);
        var isActive = false;

        // Act
        var user = new User
        {
            Id = id,
            GoogleSubjectId = googleSubjectId,
            Email = email,
            DisplayName = displayName,
            CreatedAt = createdAt,
            LastLoginAt = lastLoginAt,
            IsActive = isActive
        };

        // Assert
        user.Id.Should().Be(id);
        user.GoogleSubjectId.Should().Be(googleSubjectId);
        user.Email.Should().Be(email);
        user.DisplayName.Should().Be(displayName);
        user.CreatedAt.Should().Be(createdAt);
        user.LastLoginAt.Should().Be(lastLoginAt);
        user.IsActive.Should().BeFalse();
    }
}
