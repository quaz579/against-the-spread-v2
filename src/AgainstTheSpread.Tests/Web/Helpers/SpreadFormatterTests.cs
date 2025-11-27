using AgainstTheSpread.Core.Models;
using AgainstTheSpread.Web.Helpers;
using FluentAssertions;

namespace AgainstTheSpread.Tests.Web.Helpers;

public class SpreadFormatterTests
{
    [Fact]
    public void FormatSpreadForTeam_FavoriteWithNegativeSpread_ReturnsNegativeValue()
    {
        // Arrange
        var game = new Game
        {
            Favorite = "Michigan State",
            Underdog = "Western Michigan",
            Line = -17.5m
        };

        // Act
        var result = SpreadFormatter.FormatSpreadForTeam(game, "Michigan State");

        // Assert
        result.Should().Be("-17.5");
    }

    [Fact]
    public void FormatSpreadForTeam_UnderdogWithPositiveSpread_ReturnsPositiveValueWithPlusPrefix()
    {
        // Arrange
        var game = new Game
        {
            Favorite = "Boise State",
            Underdog = "South Florida",
            Line = -10m
        };

        // Act
        var result = SpreadFormatter.FormatSpreadForTeam(game, "South Florida");

        // Assert
        result.Should().Be("+10");
    }

    [Fact]
    public void FormatSpreadForTeam_FavoriteWithWholeNumber_ReturnsWithoutDecimal()
    {
        // Arrange
        var game = new Game
        {
            Favorite = "Iowa",
            Underdog = "Albany",
            Line = -37m
        };

        // Act
        var result = SpreadFormatter.FormatSpreadForTeam(game, "Iowa");

        // Assert
        result.Should().Be("-37");
    }

    [Fact]
    public void FormatSpreadForTeam_FavoriteWithHalfPoint_ReturnsWithOneDecimal()
    {
        // Arrange
        var game = new Game
        {
            Favorite = "Georgia",
            Underdog = "Marshall",
            Line = -38.5m
        };

        // Act
        var result = SpreadFormatter.FormatSpreadForTeam(game, "Georgia");

        // Assert
        result.Should().Be("-38.5");
    }

    [Fact]
    public void FormatSpreadForTeam_IsCaseInsensitive()
    {
        // Arrange
        var game = new Game
        {
            Favorite = "Georgia Tech",
            Underdog = "Colorado",
            Line = -3.5m
        };

        // Act
        var favoriteResult = SpreadFormatter.FormatSpreadForTeam(game, "GEORGIA TECH");
        var underdogResult = SpreadFormatter.FormatSpreadForTeam(game, "colorado");

        // Assert
        favoriteResult.Should().Be("-3.5");
        underdogResult.Should().Be("+3.5");
    }

    [Fact]
    public void FormatSpreadForTeam_ThrowsArgumentNullException_WhenGameIsNull()
    {
        // Act & Assert
        var act = () => SpreadFormatter.FormatSpreadForTeam(null!, "Team");
        act.Should().Throw<ArgumentNullException>().WithParameterName("game");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void FormatSpreadForTeam_ThrowsArgumentException_WhenTeamNameIsNullOrEmpty(string? teamName)
    {
        // Arrange
        var game = new Game
        {
            Favorite = "Test",
            Underdog = "Other",
            Line = -5m
        };

        // Act & Assert
        var act = () => SpreadFormatter.FormatSpreadForTeam(game, teamName!);
        act.Should().Throw<ArgumentException>().WithParameterName("teamName");
    }

    [Theory]
    [InlineData(-6.5, "Favorite", "-6.5")]
    [InlineData(-3, "Favorite", "-3")]
    [InlineData(-7.5, "Favorite", "-7.5")]
    [InlineData(-6.5, "Underdog", "+6.5")]
    [InlineData(-3, "Underdog", "+3")]
    [InlineData(-16.5, "Underdog", "+16.5")]
    public void FormatSpreadForTeam_FormatsCorrectly_ForVariousSpreads(decimal line, string teamType, string expected)
    {
        // Arrange
        var game = new Game
        {
            Favorite = "Favorite",
            Underdog = "Underdog",
            Line = line
        };
        var teamName = teamType;

        // Act
        var result = SpreadFormatter.FormatSpreadForTeam(game, teamName);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void FormatSpreadForTeam_TeamNotInGame_ReturnsPositiveSpread()
    {
        // Arrange - Team is treated as underdog when not matching favorite
        var game = new Game
        {
            Favorite = "Alabama",
            Underdog = "Georgia",
            Line = -5m
        };

        // Act - Unknown team is treated as not the favorite
        var result = SpreadFormatter.FormatSpreadForTeam(game, "Michigan");

        // Assert - Shows positive spread since it doesn't match favorite
        result.Should().Be("+5");
    }
}
