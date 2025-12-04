using AgainstTheSpread.Core.Models;
using FluentAssertions;
using Xunit;

namespace AgainstTheSpread.Tests.Models;

public class BowlGameTests
{
    [Fact]
    public void BowlGame_FavoriteDisplay_ReturnsFormattedString()
    {
        // Arrange
        var game = new BowlGame
        {
            Favorite = "Alabama",
            Line = -7.5m
        };

        // Act
        var display = game.FavoriteDisplay;

        // Assert
        display.Should().Be("Alabama -7.5");
    }

    [Fact]
    public void BowlGame_GameDescription_ReturnsFullDescription()
    {
        // Arrange
        var game = new BowlGame
        {
            BowlName = "Rose Bowl",
            Favorite = "Oregon",
            Line = -3.5m,
            Underdog = "Ohio State"
        };

        // Act
        var description = game.GameDescription;

        // Assert
        description.Should().Be("Rose Bowl: Oregon -3.5 vs Ohio State");
    }

    [Fact]
    public void BowlGame_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var game = new BowlGame();

        // Assert
        game.BowlName.Should().BeEmpty();
        game.GameNumber.Should().Be(0);
        game.Favorite.Should().BeEmpty();
        game.Line.Should().Be(0);
        game.Underdog.Should().BeEmpty();
    }
}
