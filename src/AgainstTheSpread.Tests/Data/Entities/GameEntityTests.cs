using AgainstTheSpread.Data.Entities;
using FluentAssertions;

namespace AgainstTheSpread.Tests.Data.Entities;

/// <summary>
/// Tests for the Game entity class used in database operations.
/// Note: This tests the Data layer entity, not the Core Game model.
/// </summary>
public class GameEntityTests
{
    [Fact]
    public void Game_NewInstance_HasDefaultId()
    {
        // Arrange & Act
        var game = new GameEntity();

        // Assert
        game.Id.Should().Be(0);
    }

    [Fact]
    public void Game_Year_DefaultsToZero()
    {
        // Arrange & Act
        var game = new GameEntity();

        // Assert
        game.Year.Should().Be(0);
    }

    [Fact]
    public void Game_Week_DefaultsToZero()
    {
        // Arrange & Act
        var game = new GameEntity();

        // Assert
        game.Week.Should().Be(0);
    }

    [Fact]
    public void Game_Favorite_DefaultsToEmptyString()
    {
        // Arrange & Act
        var game = new GameEntity();

        // Assert
        game.Favorite.Should().BeEmpty();
    }

    [Fact]
    public void Game_Underdog_DefaultsToEmptyString()
    {
        // Arrange & Act
        var game = new GameEntity();

        // Assert
        game.Underdog.Should().BeEmpty();
    }

    [Fact]
    public void Game_Line_DefaultsToZero()
    {
        // Arrange & Act
        var game = new GameEntity();

        // Assert
        game.Line.Should().Be(0);
    }

    [Fact]
    public void Game_Picks_DefaultsToEmptyCollection()
    {
        // Arrange & Act
        var game = new GameEntity();

        // Assert
        game.Picks.Should().NotBeNull();
        game.Picks.Should().BeEmpty();
    }

    [Fact]
    public void Game_ResultFields_DefaultToNull()
    {
        // Arrange & Act
        var game = new GameEntity();

        // Assert
        game.FavoriteScore.Should().BeNull();
        game.UnderdogScore.Should().BeNull();
        game.SpreadWinner.Should().BeNull();
        game.IsPush.Should().BeNull();
        game.ResultEnteredAt.Should().BeNull();
        game.ResultEnteredBy.Should().BeNull();
    }

    #region IsLocked Computed Property Tests

    [Fact]
    public void IsLocked_WhenGameDateInPast_ReturnsTrue()
    {
        // Arrange
        var game = new GameEntity
        {
            GameDate = DateTime.UtcNow.AddHours(-1)
        };

        // Act
        var isLocked = game.IsLocked;

        // Assert
        isLocked.Should().BeTrue();
    }

    [Fact]
    public void IsLocked_WhenGameDateIsNow_ReturnsTrue()
    {
        // Arrange
        var game = new GameEntity
        {
            GameDate = DateTime.UtcNow
        };

        // Act
        var isLocked = game.IsLocked;

        // Assert
        isLocked.Should().BeTrue();
    }

    [Fact]
    public void IsLocked_WhenGameDateInFuture_ReturnsFalse()
    {
        // Arrange
        var game = new GameEntity
        {
            GameDate = DateTime.UtcNow.AddHours(1)
        };

        // Act
        var isLocked = game.IsLocked;

        // Assert
        isLocked.Should().BeFalse();
    }

    [Fact]
    public void IsLocked_WhenGameDateIsWayInPast_ReturnsTrue()
    {
        // Arrange
        var game = new GameEntity
        {
            GameDate = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc)
        };

        // Act
        var isLocked = game.IsLocked;

        // Assert
        isLocked.Should().BeTrue();
    }

    [Fact]
    public void IsLocked_WhenGameDateIsWayInFuture_ReturnsFalse()
    {
        // Arrange
        var game = new GameEntity
        {
            GameDate = new DateTime(2099, 12, 31, 23, 59, 59, DateTimeKind.Utc)
        };

        // Act
        var isLocked = game.IsLocked;

        // Assert
        isLocked.Should().BeFalse();
    }

    #endregion

    #region HasResult Computed Property Tests

    [Fact]
    public void HasResult_WhenNoResultSet_ReturnsFalse()
    {
        // Arrange
        var game = new GameEntity();

        // Act
        var hasResult = game.HasResult;

        // Assert
        hasResult.Should().BeFalse();
    }

    [Fact]
    public void HasResult_WhenSpreadWinnerIsSet_ReturnsTrue()
    {
        // Arrange
        var game = new GameEntity
        {
            SpreadWinner = "Alabama"
        };

        // Act
        var hasResult = game.HasResult;

        // Assert
        hasResult.Should().BeTrue();
    }

    [Fact]
    public void HasResult_WhenIsPushIsTrue_ReturnsTrue()
    {
        // Arrange
        var game = new GameEntity
        {
            IsPush = true
        };

        // Act
        var hasResult = game.HasResult;

        // Assert
        hasResult.Should().BeTrue();
    }

    [Fact]
    public void HasResult_WhenIsPushIsFalse_ReturnsFalse()
    {
        // Arrange - IsPush = false means the result was entered but it wasn't a push
        // However, if IsPush is explicitly false, we should have a SpreadWinner
        var game = new GameEntity
        {
            IsPush = false
        };

        // Act
        var hasResult = game.HasResult;

        // Assert
        hasResult.Should().BeFalse();
    }

    [Fact]
    public void HasResult_WhenBothSpreadWinnerAndIsPushSet_ReturnsTrue()
    {
        // Arrange - edge case, but should still work
        var game = new GameEntity
        {
            SpreadWinner = "Georgia",
            IsPush = false
        };

        // Act
        var hasResult = game.HasResult;

        // Assert
        hasResult.Should().BeTrue();
    }

    [Fact]
    public void HasResult_WhenOnlyScoresSet_ReturnsFalse()
    {
        // Arrange - Scores alone don't mean the game has a result processed
        var game = new GameEntity
        {
            FavoriteScore = 35,
            UnderdogScore = 21
        };

        // Act
        var hasResult = game.HasResult;

        // Assert
        hasResult.Should().BeFalse();
    }

    #endregion

    [Fact]
    public void Game_CanSetAllProperties()
    {
        // Arrange
        var gameDate = new DateTime(2024, 9, 7, 19, 0, 0, DateTimeKind.Utc);
        var resultEnteredAt = new DateTime(2024, 9, 7, 23, 0, 0, DateTimeKind.Utc);
        var resultEnteredBy = Guid.NewGuid();

        // Act
        var game = new GameEntity
        {
            Id = 1,
            Year = 2024,
            Week = 1,
            Favorite = "Alabama",
            Underdog = "Auburn",
            Line = -7.5m,
            GameDate = gameDate,
            FavoriteScore = 35,
            UnderdogScore = 21,
            SpreadWinner = "Alabama",
            IsPush = false,
            ResultEnteredAt = resultEnteredAt,
            ResultEnteredBy = resultEnteredBy
        };

        // Assert
        game.Id.Should().Be(1);
        game.Year.Should().Be(2024);
        game.Week.Should().Be(1);
        game.Favorite.Should().Be("Alabama");
        game.Underdog.Should().Be("Auburn");
        game.Line.Should().Be(-7.5m);
        game.GameDate.Should().Be(gameDate);
        game.FavoriteScore.Should().Be(35);
        game.UnderdogScore.Should().Be(21);
        game.SpreadWinner.Should().Be("Alabama");
        game.IsPush.Should().BeFalse();
        game.ResultEnteredAt.Should().Be(resultEnteredAt);
        game.ResultEnteredBy.Should().Be(resultEnteredBy);
    }
}
