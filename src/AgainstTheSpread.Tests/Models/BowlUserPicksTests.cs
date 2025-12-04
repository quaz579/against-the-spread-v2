using AgainstTheSpread.Core.Models;
using FluentAssertions;
using Xunit;

namespace AgainstTheSpread.Tests.Models;

public class BowlUserPicksTests
{
    private List<BowlPick> CreateValidPicks(int totalGames)
    {
        var picks = new List<BowlPick>();
        for (int i = 1; i <= totalGames; i++)
        {
            picks.Add(new BowlPick
            {
                GameNumber = i,
                SpreadPick = $"Team{i}",
                ConfidencePoints = i,
                OutrightWinner = $"Team{i}"
            });
        }
        return picks;
    }

    [Fact]
    public void BowlUserPicks_ExpectedConfidenceSum_CalculatesCorrectly()
    {
        // Arrange
        var picks = new BowlUserPicks { TotalGames = 36 };

        // Act
        var expectedSum = picks.ExpectedConfidenceSum;

        // Assert
        // 36 * 37 / 2 = 666
        expectedSum.Should().Be(666);
    }

    [Fact]
    public void BowlUserPicks_ExpectedConfidenceSum_For37Games()
    {
        // Arrange
        var picks = new BowlUserPicks { TotalGames = 37 };

        // Act
        var expectedSum = picks.ExpectedConfidenceSum;

        // Assert
        // 37 * 38 / 2 = 703
        expectedSum.Should().Be(703);
    }

    [Fact]
    public void BowlUserPicks_IsValid_ReturnsTrueForValidPicks()
    {
        // Arrange
        var userPicks = new BowlUserPicks
        {
            Name = "John Doe",
            Year = 2024,
            TotalGames = 36,
            Picks = CreateValidPicks(36)
        };

        // Act
        var isValid = userPicks.IsValid();

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void BowlUserPicks_IsValid_ReturnsFalseForEmptyName()
    {
        // Arrange
        var userPicks = new BowlUserPicks
        {
            Name = "",
            Year = 2024,
            TotalGames = 36,
            Picks = CreateValidPicks(36)
        };

        // Act
        var isValid = userPicks.IsValid();

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void BowlUserPicks_IsValid_ReturnsFalseForInvalidYear()
    {
        // Arrange
        var userPicks = new BowlUserPicks
        {
            Name = "John Doe",
            Year = 2019,
            TotalGames = 36,
            Picks = CreateValidPicks(36)
        };

        // Act
        var isValid = userPicks.IsValid();

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void BowlUserPicks_IsValid_ReturnsFalseForZeroTotalGames()
    {
        // Arrange
        var userPicks = new BowlUserPicks
        {
            Name = "John Doe",
            Year = 2024,
            TotalGames = 0,
            Picks = new List<BowlPick>()
        };

        // Act
        var isValid = userPicks.IsValid();

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void BowlUserPicks_IsValid_ReturnsFalseForWrongPickCount()
    {
        // Arrange
        var userPicks = new BowlUserPicks
        {
            Name = "John Doe",
            Year = 2024,
            TotalGames = 36,
            Picks = CreateValidPicks(35) // Missing one pick
        };

        // Act
        var isValid = userPicks.IsValid();

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void BowlUserPicks_IsValid_ReturnsFalseForDuplicateConfidencePoints()
    {
        // Arrange
        var picks = CreateValidPicks(36);
        picks[0].ConfidencePoints = 36;
        picks[1].ConfidencePoints = 36; // Duplicate
        
        var userPicks = new BowlUserPicks
        {
            Name = "John Doe",
            Year = 2024,
            TotalGames = 36,
            Picks = picks
        };

        // Act
        var isValid = userPicks.IsValid();

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void BowlUserPicks_GetValidationError_ReturnsNullForValidPicks()
    {
        // Arrange
        var userPicks = new BowlUserPicks
        {
            Name = "John Doe",
            Year = 2024,
            TotalGames = 36,
            Picks = CreateValidPicks(36)
        };

        // Act
        var error = userPicks.GetValidationError();

        // Assert
        error.Should().BeNull();
    }

    [Fact]
    public void BowlUserPicks_GetValidationError_ReturnsErrorForEmptyName()
    {
        // Arrange
        var userPicks = new BowlUserPicks
        {
            Name = "",
            Year = 2024,
            TotalGames = 36,
            Picks = CreateValidPicks(36)
        };

        // Act
        var error = userPicks.GetValidationError();

        // Assert
        error.Should().Be("Name is required");
    }

    [Fact]
    public void BowlUserPicks_GetValidationError_ReturnsErrorForInvalidYear()
    {
        // Arrange
        var userPicks = new BowlUserPicks
        {
            Name = "John Doe",
            Year = 2019,
            TotalGames = 36,
            Picks = CreateValidPicks(36)
        };

        // Act
        var error = userPicks.GetValidationError();

        // Assert
        error.Should().Be("Invalid year");
    }

    [Fact]
    public void BowlUserPicks_GetValidationError_ReturnsErrorForDuplicateConfidence()
    {
        // Arrange
        var picks = CreateValidPicks(36);
        picks[0].ConfidencePoints = 36;
        picks[1].ConfidencePoints = 36; // Duplicate
        
        var userPicks = new BowlUserPicks
        {
            Name = "John Doe",
            Year = 2024,
            TotalGames = 36,
            Picks = picks
        };

        // Act
        var error = userPicks.GetValidationError();

        // Assert
        error.Should().Contain("Duplicate confidence points");
    }

    [Fact]
    public void BowlUserPicks_HasValidConfidenceSum_ReturnsTrueForValidSum()
    {
        // Arrange
        var userPicks = new BowlUserPicks
        {
            TotalGames = 36,
            Picks = CreateValidPicks(36)
        };

        // Act
        var hasValidSum = userPicks.HasValidConfidenceSum();

        // Assert
        hasValidSum.Should().BeTrue();
    }

    [Fact]
    public void BowlUserPicks_HasValidConfidenceSum_ReturnsFalseForInvalidSum()
    {
        // Arrange
        var picks = CreateValidPicks(36);
        picks[0].ConfidencePoints = 100; // Invalid
        
        var userPicks = new BowlUserPicks
        {
            TotalGames = 36,
            Picks = picks
        };

        // Act
        var hasValidSum = userPicks.HasValidConfidenceSum();

        // Assert
        hasValidSum.Should().BeFalse();
    }
}
