using AgainstTheSpread.Core.Models;
using FluentAssertions;

namespace AgainstTheSpread.Tests.Models;

public class GameTests
{
    [Fact]
    public void FavoriteDisplay_ReturnsFormattedString()
    {
        // Arrange
        var game = new Game
        {
            Favorite = "Alabama",
            Line = -7.5m,
            Underdog = "Auburn",
            VsAt = "vs"
        };

        // Act
        var display = game.FavoriteDisplay;

        // Assert
        display.Should().Be("Alabama -7.5");
    }

    [Fact]
    public void UnderdogDisplay_ReturnsTeamName()
    {
        // Arrange
        var game = new Game
        {
            Favorite = "Alabama",
            Line = -7.5m,
            Underdog = "Auburn",
            VsAt = "vs"
        };

        // Act
        var display = game.UnderdogDisplay;

        // Assert
        display.Should().Be("Auburn");
    }

    [Fact]
    public void GameDescription_WithVs_ReturnsCorrectFormat()
    {
        // Arrange
        var game = new Game
        {
            Favorite = "Alabama",
            Line = -7.5m,
            Underdog = "Auburn",
            VsAt = "vs"
        };

        // Act
        var display = game.GameDescription;

        // Assert
        display.Should().Be("Alabama -7.5 vs Auburn");
    }

    [Fact]
    public void GameDescription_WithAt_ReturnsCorrectFormat()
    {
        // Arrange
        var game = new Game
        {
            Favorite = "Alabama",
            Line = -14.0m,
            Underdog = "Vanderbilt",
            VsAt = "@"
        };

        // Act
        var display = game.GameDescription;

        // Assert
        display.Should().Be("Alabama -14.0 @ Vanderbilt");
    }

    [Theory]
    [InlineData(-7.5, "Alabama -7.5")]
    [InlineData(-14.0, "Alabama -14")]
    [InlineData(-3.5, "Alabama -3.5")]
    [InlineData(-10.0, "Alabama -10")]
    public void FavoriteDisplay_WithVariousLines_ReturnsCorrectFormat(double line, string expected)
    {
        // Arrange
        var game = new Game
        {
            Favorite = "Alabama",
            Line = (decimal)line,
            Underdog = "Auburn",
            VsAt = "vs"
        };

        // Act
        var display = game.FavoriteDisplay;

        // Assert
        display.Should().Be(expected);
    }

    [Fact]
    public void GameDate_CanBeSet()
    {
        // Arrange
        var expectedDate = new DateTime(2024, 9, 7, 19, 0, 0);
        var game = new Game
        {
            Favorite = "Alabama",
            Line = -7.5m,
            Underdog = "Auburn",
            VsAt = "vs",
            GameDate = expectedDate
        };

        // Act & Assert
        game.GameDate.Should().Be(expectedDate);
    }

    [Fact]
    public void VsAt_DefaultsToEmptyString()
    {
        // Arrange & Act
        var game = new Game();

        // Assert
        game.VsAt.Should().Be(string.Empty);
    }

    [Fact]
    public void Favorite_DefaultsToEmptyString()
    {
        // Arrange & Act
        var game = new Game();

        // Assert
        game.Favorite.Should().Be(string.Empty);
    }

    [Fact]
    public void Underdog_DefaultsToEmptyString()
    {
        // Arrange & Act
        var game = new Game();

        // Assert
        game.Underdog.Should().Be(string.Empty);
    }

    [Fact]
    public void Line_DefaultsToZero()
    {
        // Arrange & Act
        var game = new Game();

        // Assert
        game.Line.Should().Be(0);
    }
}
