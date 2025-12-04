using AgainstTheSpread.Core.Models;
using FluentAssertions;
using Xunit;

namespace AgainstTheSpread.Tests.Models;

public class BowlLinesTests
{
    [Fact]
    public void BowlLines_TotalGames_ReturnsGameCount()
    {
        // Arrange
        var bowlLines = new BowlLines
        {
            Games = new List<BowlGame>
            {
                new BowlGame { GameNumber = 1, BowlName = "Rose Bowl" },
                new BowlGame { GameNumber = 2, BowlName = "Sugar Bowl" },
                new BowlGame { GameNumber = 3, BowlName = "Orange Bowl" }
            }
        };

        // Act
        var totalGames = bowlLines.TotalGames;

        // Assert
        totalGames.Should().Be(3);
    }

    [Fact]
    public void BowlLines_IsValid_ReturnsTrueForValidLines()
    {
        // Arrange
        var bowlLines = new BowlLines
        {
            Year = 2024,
            Games = new List<BowlGame>
            {
                new BowlGame { GameNumber = 1, BowlName = "Rose Bowl" }
            }
        };

        // Act
        var isValid = bowlLines.IsValid();

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void BowlLines_IsValid_ReturnsFalseForEmptyGames()
    {
        // Arrange
        var bowlLines = new BowlLines
        {
            Year = 2024,
            Games = new List<BowlGame>()
        };

        // Act
        var isValid = bowlLines.IsValid();

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void BowlLines_IsValid_ReturnsFalseForInvalidYear()
    {
        // Arrange
        var bowlLines = new BowlLines
        {
            Year = 2019,
            Games = new List<BowlGame>
            {
                new BowlGame { GameNumber = 1, BowlName = "Rose Bowl" }
            }
        };

        // Act
        var isValid = bowlLines.IsValid();

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void BowlLines_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var bowlLines = new BowlLines();

        // Assert
        bowlLines.Year.Should().Be(0);
        bowlLines.Games.Should().NotBeNull();
        bowlLines.Games.Should().BeEmpty();
        bowlLines.TotalGames.Should().Be(0);
    }
}
