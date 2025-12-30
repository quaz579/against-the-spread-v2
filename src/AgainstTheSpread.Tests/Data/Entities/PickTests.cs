using AgainstTheSpread.Data.Entities;
using FluentAssertions;

namespace AgainstTheSpread.Tests.Data.Entities;

public class PickTests
{
    [Fact]
    public void Pick_NewInstance_HasDefaultId()
    {
        // Arrange & Act
        var pick = new Pick();

        // Assert
        pick.Id.Should().Be(0);
    }

    [Fact]
    public void Pick_UserId_DefaultsToEmptyGuid()
    {
        // Arrange & Act
        var pick = new Pick();

        // Assert
        pick.UserId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void Pick_GameId_DefaultsToZero()
    {
        // Arrange & Act
        var pick = new Pick();

        // Assert
        pick.GameId.Should().Be(0);
    }

    [Fact]
    public void Pick_SelectedTeam_DefaultsToEmptyString()
    {
        // Arrange & Act
        var pick = new Pick();

        // Assert
        pick.SelectedTeam.Should().BeEmpty();
    }

    [Fact]
    public void Pick_Year_DefaultsToZero()
    {
        // Arrange & Act
        var pick = new Pick();

        // Assert
        pick.Year.Should().Be(0);
    }

    [Fact]
    public void Pick_Week_DefaultsToZero()
    {
        // Arrange & Act
        var pick = new Pick();

        // Assert
        pick.Week.Should().Be(0);
    }

    [Fact]
    public void Pick_SubmittedAt_DefaultsToMinValue()
    {
        // Arrange & Act
        var pick = new Pick();

        // Assert
        pick.SubmittedAt.Should().Be(default(DateTime));
    }

    [Fact]
    public void Pick_UpdatedAt_DefaultsToNull()
    {
        // Arrange & Act
        var pick = new Pick();

        // Assert
        pick.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Pick_CanSetAllProperties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var submittedAt = new DateTime(2024, 9, 5, 10, 0, 0, DateTimeKind.Utc);
        var updatedAt = new DateTime(2024, 9, 5, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var pick = new Pick
        {
            Id = 1,
            UserId = userId,
            GameId = 42,
            SelectedTeam = "Alabama",
            SubmittedAt = submittedAt,
            UpdatedAt = updatedAt,
            Year = 2024,
            Week = 1
        };

        // Assert
        pick.Id.Should().Be(1);
        pick.UserId.Should().Be(userId);
        pick.GameId.Should().Be(42);
        pick.SelectedTeam.Should().Be("Alabama");
        pick.SubmittedAt.Should().Be(submittedAt);
        pick.UpdatedAt.Should().Be(updatedAt);
        pick.Year.Should().Be(2024);
        pick.Week.Should().Be(1);
    }

    [Fact]
    public void Pick_NavigationProperties_AreInitialized()
    {
        // Arrange & Act
        var pick = new Pick();

        // Assert - Navigation properties should be null by default (not initialized)
        // EF Core will populate them when needed
        pick.User.Should().BeNull();
        pick.Game.Should().BeNull();
    }

    [Fact]
    public void Pick_CanSetNavigationProperties()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            DisplayName = "Test User"
        };

        var game = new GameEntity
        {
            Id = 1,
            Year = 2024,
            Week = 1,
            Favorite = "Alabama",
            Underdog = "Auburn"
        };

        // Act
        var pick = new Pick
        {
            UserId = user.Id,
            GameId = game.Id,
            User = user,
            Game = game,
            SelectedTeam = "Alabama"
        };

        // Assert
        pick.User.Should().Be(user);
        pick.Game.Should().Be(game);
        pick.UserId.Should().Be(user.Id);
        pick.GameId.Should().Be(game.Id);
    }

    [Fact]
    public void Pick_SelectedTeam_ShouldMatchFavoriteOrUnderdog()
    {
        // Arrange
        var game = new GameEntity
        {
            Id = 1,
            Favorite = "Alabama",
            Underdog = "Auburn"
        };

        // Act - Pick the favorite
        var pickFavorite = new Pick
        {
            GameId = game.Id,
            Game = game,
            SelectedTeam = "Alabama"
        };

        // Act - Pick the underdog
        var pickUnderdog = new Pick
        {
            GameId = game.Id,
            Game = game,
            SelectedTeam = "Auburn"
        };

        // Assert
        pickFavorite.SelectedTeam.Should().Be(game.Favorite);
        pickUnderdog.SelectedTeam.Should().Be(game.Underdog);
    }
}
