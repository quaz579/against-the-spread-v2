using AgainstTheSpread.Core.Models;
using FluentAssertions;

namespace AgainstTheSpread.Tests.Models;

public class WeeklyLinesTests
{
    [Fact]
    public void IsValid_WithValidData_ReturnsTrue()
    {
        // Arrange
        var lines = new WeeklyLines
        {
            Week = 1,
            Year = 2024,
            Games = new List<Game>
            {
                new() { Favorite = "Team A", Line = -7.0m, Underdog = "Team B", VsAt = "@" }
            }
        };

        // Act
        var result = lines.IsValid();

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(15)]
    [InlineData(100)]
    public void IsValid_WithInvalidWeek_ReturnsFalse(int week)
    {
        // Arrange
        var lines = new WeeklyLines
        {
            Week = week,
            Year = 2024,
            Games = new List<Game>
            {
                new() { Favorite = "Team A", Line = -7.0m, Underdog = "Team B", VsAt = "@" }
            }
        };

        // Act
        var result = lines.IsValid();

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(2019)]
    [InlineData(1999)]
    [InlineData(0)]
    public void IsValid_WithInvalidYear_ReturnsFalse(int year)
    {
        // Arrange
        var lines = new WeeklyLines
        {
            Week = 1,
            Year = year,
            Games = new List<Game>
            {
                new() { Favorite = "Team A", Line = -7.0m, Underdog = "Team B", VsAt = "@" }
            }
        };

        // Act
        var result = lines.IsValid();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithEmptyGames_ReturnsFalse()
    {
        // Arrange
        var lines = new WeeklyLines
        {
            Week = 1,
            Year = 2024,
            Games = new List<Game>()
        };

        // Act
        var result = lines.IsValid();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithNullGames_ReturnsFalse()
    {
        // Arrange
        var lines = new WeeklyLines
        {
            Week = 1,
            Year = 2024,
            Games = null!
        };

        // Act
        var result = lines.IsValid();

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(14)]
    public void IsValid_WithValidWeekRange_ReturnsTrue(int week)
    {
        // Arrange
        var lines = new WeeklyLines
        {
            Week = week,
            Year = 2024,
            Games = new List<Game>
            {
                new() { Favorite = "Team A", Line = -7.0m, Underdog = "Team B", VsAt = "@" }
            }
        };

        // Act
        var result = lines.IsValid();

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(2020)]
    [InlineData(2024)]
    [InlineData(2030)]
    public void IsValid_WithValidYear_ReturnsTrue(int year)
    {
        // Arrange
        var lines = new WeeklyLines
        {
            Week = 1,
            Year = year,
            Games = new List<Game>
            {
                new() { Favorite = "Team A", Line = -7.0m, Underdog = "Team B", VsAt = "@" }
            }
        };

        // Act
        var result = lines.IsValid();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Games_DefaultsToEmptyList()
    {
        // Arrange & Act
        var lines = new WeeklyLines();

        // Assert
        lines.Games.Should().NotBeNull();
        lines.Games.Should().BeEmpty();
    }
}
