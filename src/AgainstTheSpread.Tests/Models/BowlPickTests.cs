using AgainstTheSpread.Core.Models;
using FluentAssertions;
using Xunit;

namespace AgainstTheSpread.Tests.Models;

public class BowlPickTests
{
    [Fact]
    public void BowlPick_IsValid_ReturnsTrueForValidPick()
    {
        // Arrange
        var pick = new BowlPick
        {
            GameNumber = 1,
            SpreadPick = "Alabama",
            ConfidencePoints = 36,
            OutrightWinner = "Alabama"
        };

        // Act
        var isValid = pick.IsValid(36);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void BowlPick_IsValid_ReturnsFalseForInvalidGameNumber()
    {
        // Arrange
        var pick = new BowlPick
        {
            GameNumber = 0,
            SpreadPick = "Alabama",
            ConfidencePoints = 36,
            OutrightWinner = "Alabama"
        };

        // Act
        var isValid = pick.IsValid(36);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void BowlPick_IsValid_ReturnsFalseForGameNumberOverLimit()
    {
        // Arrange
        var pick = new BowlPick
        {
            GameNumber = 37,
            SpreadPick = "Alabama",
            ConfidencePoints = 36,
            OutrightWinner = "Alabama"
        };

        // Act
        var isValid = pick.IsValid(36);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void BowlPick_IsValid_ReturnsFalseForEmptySpreadPick()
    {
        // Arrange
        var pick = new BowlPick
        {
            GameNumber = 1,
            SpreadPick = "",
            ConfidencePoints = 36,
            OutrightWinner = "Alabama"
        };

        // Act
        var isValid = pick.IsValid(36);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void BowlPick_IsValid_ReturnsFalseForInvalidConfidencePoints()
    {
        // Arrange
        var pick = new BowlPick
        {
            GameNumber = 1,
            SpreadPick = "Alabama",
            ConfidencePoints = 0,
            OutrightWinner = "Alabama"
        };

        // Act
        var isValid = pick.IsValid(36);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void BowlPick_IsValid_ReturnsFalseForConfidencePointsOverLimit()
    {
        // Arrange
        var pick = new BowlPick
        {
            GameNumber = 1,
            SpreadPick = "Alabama",
            ConfidencePoints = 37,
            OutrightWinner = "Alabama"
        };

        // Act
        var isValid = pick.IsValid(36);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void BowlPick_IsValid_ReturnsFalseForEmptyOutrightWinner()
    {
        // Arrange
        var pick = new BowlPick
        {
            GameNumber = 1,
            SpreadPick = "Alabama",
            ConfidencePoints = 36,
            OutrightWinner = ""
        };

        // Act
        var isValid = pick.IsValid(36);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void BowlPick_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var pick = new BowlPick();

        // Assert
        pick.GameNumber.Should().Be(0);
        pick.SpreadPick.Should().BeEmpty();
        pick.ConfidencePoints.Should().Be(0);
        pick.OutrightWinner.Should().BeEmpty();
    }
}
