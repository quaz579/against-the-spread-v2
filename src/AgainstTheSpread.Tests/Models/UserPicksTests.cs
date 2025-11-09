using AgainstTheSpread.Core.Models;
using FluentAssertions;

namespace AgainstTheSpread.Tests.Models;

public class UserPicksTests
{
    [Fact]
    public void IsValid_WithValidData_ReturnsTrue()
    {
        // Arrange
        var picks = new UserPicks
        {
            Name = "John Doe",
            Week = 1,
            Year = 2024,
            Picks = new List<string> { "Team1", "Team2", "Team3", "Team4", "Team5", "Team6" },
            SubmittedAt = DateTime.UtcNow
        };

        // Act
        var result = picks.IsValid();

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void IsValid_WithInvalidName_ReturnsFalse(string? name)
    {
        // Arrange
        var picks = new UserPicks
        {
            Name = name!,
            Week = 1,
            Year = 2024,
            Picks = new List<string> { "Team1", "Team2", "Team3", "Team4", "Team5", "Team6" }
        };

        // Act
        var result = picks.IsValid();

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(15)]
    [InlineData(100)]
    public void IsValid_WithInvalidWeek_ReturnsFalse(int week)
    {
        // Arrange
        var picks = new UserPicks
        {
            Name = "John Doe",
            Week = week,
            Year = 2024,
            Picks = new List<string> { "Team1", "Team2", "Team3", "Team4", "Team5", "Team6" }
        };

        // Act
        var result = picks.IsValid();

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(5)]
    [InlineData(7)]
    [InlineData(0)]
    [InlineData(10)]
    public void IsValid_WithIncorrectPickCount_ReturnsFalse(int pickCount)
    {
        // Arrange
        var picks = new UserPicks
        {
            Name = "John Doe",
            Week = 1,
            Year = 2024,
            Picks = Enumerable.Range(1, pickCount).Select(i => $"Team{i}").ToList()
        };

        // Act
        var result = picks.IsValid();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithEmptyPickInList_ReturnsFalse()
    {
        // Arrange
        var picks = new UserPicks
        {
            Name = "John Doe",
            Week = 1,
            Year = 2024,
            Picks = new List<string> { "Team1", "Team2", "", "Team4", "Team5", "Team6" }
        };

        // Act
        var result = picks.IsValid();

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(2019)]
    [InlineData(2000)]
    [InlineData(1999)]
    public void IsValid_WithInvalidYear_ReturnsFalse(int year)
    {
        // Arrange
        var picks = new UserPicks
        {
            Name = "John Doe",
            Week = 1,
            Year = year,
            Picks = new List<string> { "Team1", "Team2", "Team3", "Team4", "Team5", "Team6" }
        };

        // Act
        var result = picks.IsValid();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetValidationError_WithValidData_ReturnsNull()
    {
        // Arrange
        var picks = new UserPicks
        {
            Name = "John Doe",
            Week = 1,
            Year = 2024,
            Picks = new List<string> { "Team1", "Team2", "Team3", "Team4", "Team5", "Team6" }
        };

        // Act
        var error = picks.GetValidationError();

        // Assert
        error.Should().BeNull();
    }

    [Fact]
    public void GetValidationError_WithEmptyName_ReturnsNameError()
    {
        // Arrange
        var picks = new UserPicks
        {
            Name = "",
            Week = 1,
            Year = 2024,
            Picks = new List<string> { "Team1", "Team2", "Team3", "Team4", "Team5", "Team6" }
        };

        // Act
        var error = picks.GetValidationError();

        // Assert
        error.Should().Be("Name is required");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    public void GetValidationError_WithInvalidWeek_ReturnsWeekError(int week)
    {
        // Arrange
        var picks = new UserPicks
        {
            Name = "John Doe",
            Week = week,
            Year = 2024,
            Picks = new List<string> { "Team1", "Team2", "Team3", "Team4", "Team5", "Team6" }
        };

        // Act
        var error = picks.GetValidationError();

        // Assert
        error.Should().Be("Week must be between 1 and 14");
    }

    [Theory]
    [InlineData(5, "Exactly 6 picks are required (you have 5)")]
    [InlineData(7, "Exactly 6 picks are required (you have 7)")]
    [InlineData(0, "Exactly 6 picks are required (you have 0)")]
    public void GetValidationError_WithIncorrectPickCount_ReturnsPickCountError(int pickCount, string expectedError)
    {
        // Arrange
        var picks = new UserPicks
        {
            Name = "John Doe",
            Week = 1,
            Year = 2024,
            Picks = Enumerable.Range(1, pickCount).Select(i => $"Team{i}").ToList()
        };

        // Act
        var error = picks.GetValidationError();

        // Assert
        error.Should().Be(expectedError);
    }

    [Fact]
    public void GetValidationError_WithEmptyPick_ReturnsTeamNameError()
    {
        // Arrange
        var picks = new UserPicks
        {
            Name = "John Doe",
            Week = 1,
            Year = 2024,
            Picks = new List<string> { "Team1", "", "Team3", "Team4", "Team5", "Team6" }
        };

        // Act
        var error = picks.GetValidationError();

        // Assert
        error.Should().Be("All picks must have a team name");
    }

    [Fact]
    public void GetValidationError_WithInvalidYear_ReturnsYearError()
    {
        // Arrange
        var picks = new UserPicks
        {
            Name = "John Doe",
            Week = 1,
            Year = 2019,
            Picks = new List<string> { "Team1", "Team2", "Team3", "Team4", "Team5", "Team6" }
        };

        // Act
        var error = picks.GetValidationError();

        // Assert
        error.Should().Be("Invalid year");
    }

    [Fact]
    public void RequiredPickCount_ShouldBeSix()
    {
        // Assert
        UserPicks.RequiredPickCount.Should().Be(6);
    }
}
