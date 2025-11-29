using AgainstTheSpread.Core.Models;
using AgainstTheSpread.Web.Helpers;
using FluentAssertions;

namespace AgainstTheSpread.Tests.Web.Helpers;

public class PickSelectionHelperTests
{
    [Fact]
    public void TogglePick_SelectsTeam_WhenNotAlreadySelected()
    {
        // Arrange
        var selectedPicks = new List<string>();
        var games = new List<Game>
        {
            new Game { Favorite = "Alabama", Underdog = "Georgia", Line = -7m }
        };

        // Act
        PickSelectionHelper.TogglePick("Alabama", selectedPicks, games);

        // Assert
        selectedPicks.Should().ContainSingle().Which.Should().Be("Alabama");
    }

    [Fact]
    public void TogglePick_DeselectsTeam_WhenAlreadySelected()
    {
        // Arrange
        var selectedPicks = new List<string> { "Alabama" };
        var games = new List<Game>
        {
            new Game { Favorite = "Alabama", Underdog = "Georgia", Line = -7m }
        };

        // Act
        PickSelectionHelper.TogglePick("Alabama", selectedPicks, games);

        // Assert
        selectedPicks.Should().BeEmpty();
    }

    [Fact]
    public void TogglePick_AutoDeselectsOppositeTeam_WhenSelectingFavorite()
    {
        // Arrange
        var selectedPicks = new List<string> { "Georgia" };
        var games = new List<Game>
        {
            new Game { Favorite = "Alabama", Underdog = "Georgia", Line = -7m }
        };

        // Act
        PickSelectionHelper.TogglePick("Alabama", selectedPicks, games);

        // Assert
        selectedPicks.Should().ContainSingle().Which.Should().Be("Alabama");
        selectedPicks.Should().NotContain("Georgia");
    }

    [Fact]
    public void TogglePick_AutoDeselectsOppositeTeam_WhenSelectingUnderdog()
    {
        // Arrange
        var selectedPicks = new List<string> { "Alabama" };
        var games = new List<Game>
        {
            new Game { Favorite = "Alabama", Underdog = "Georgia", Line = -7m }
        };

        // Act
        PickSelectionHelper.TogglePick("Georgia", selectedPicks, games);

        // Assert
        selectedPicks.Should().ContainSingle().Which.Should().Be("Georgia");
        selectedPicks.Should().NotContain("Alabama");
    }

    [Fact]
    public void TogglePick_OnlyAffectsSameGame_WhenSelectingTeam()
    {
        // Arrange - Two different games
        var selectedPicks = new List<string> { "Michigan", "Georgia" };
        var games = new List<Game>
        {
            new Game { Favorite = "Alabama", Underdog = "Georgia", Line = -7m },
            new Game { Favorite = "Michigan", Underdog = "Ohio State", Line = -3m }
        };

        // Act - Select Alabama, should only deselect Georgia (same game), not Michigan
        PickSelectionHelper.TogglePick("Alabama", selectedPicks, games);

        // Assert
        selectedPicks.Should().HaveCount(2);
        selectedPicks.Should().Contain("Alabama");
        selectedPicks.Should().Contain("Michigan");
        selectedPicks.Should().NotContain("Georgia");
    }

    [Fact]
    public void TogglePick_RespectsMaxPicksLimit()
    {
        // Arrange
        var selectedPicks = new List<string> { "Team1", "Team2", "Team3", "Team4", "Team5", "Team6" };
        var games = new List<Game>
        {
            new Game { Favorite = "NewTeam", Underdog = "OtherTeam", Line = -5m }
        };

        // Act - Try to add 7th pick without deselecting
        PickSelectionHelper.TogglePick("NewTeam", selectedPicks, games);

        // Assert - Should still have 6 picks
        selectedPicks.Should().HaveCount(6);
        selectedPicks.Should().NotContain("NewTeam");
    }

    [Fact]
    public void TogglePick_CanAddPick_WhenAutoDeselectCreatesRoom()
    {
        // Arrange - 6 picks including the opposite team
        var selectedPicks = new List<string> { "Team1", "Team2", "Team3", "Team4", "Team5", "Georgia" };
        var games = new List<Game>
        {
            new Game { Favorite = "Alabama", Underdog = "Georgia", Line = -7m }
        };

        // Act - Select Alabama should auto-deselect Georgia and add Alabama
        PickSelectionHelper.TogglePick("Alabama", selectedPicks, games);

        // Assert - Should still have 6 picks, but with Alabama instead of Georgia
        selectedPicks.Should().HaveCount(6);
        selectedPicks.Should().Contain("Alabama");
        selectedPicks.Should().NotContain("Georgia");
    }

    [Fact]
    public void TogglePick_IsCaseInsensitive_WhenFindingGame()
    {
        // Arrange - Note: selectedPicks uses case-sensitive matching, but game lookup is case-insensitive
        var selectedPicks = new List<string> { "Georgia" }; // Match the exact case from game
        var games = new List<Game>
        {
            new Game { Favorite = "Alabama", Underdog = "Georgia", Line = -7m }
        };

        // Act - lowercase team name should still find the game
        PickSelectionHelper.TogglePick("alabama", selectedPicks, games);

        // Assert - Georgia was removed (matched by game lookup), alabama was added
        selectedPicks.Should().ContainSingle().Which.Should().Be("alabama");
    }

    [Fact]
    public void TogglePick_HandlesNullGames_Gracefully()
    {
        // Arrange
        var selectedPicks = new List<string>();

        // Act - Should not throw
        PickSelectionHelper.TogglePick("Team", selectedPicks, null);

        // Assert - Should still add the team
        selectedPicks.Should().ContainSingle().Which.Should().Be("Team");
    }

    [Fact]
    public void TogglePick_HandlesEmptyGames_Gracefully()
    {
        // Arrange
        var selectedPicks = new List<string>();
        var games = new List<Game>();

        // Act
        PickSelectionHelper.TogglePick("Team", selectedPicks, games);

        // Assert
        selectedPicks.Should().ContainSingle().Which.Should().Be("Team");
    }

    [Fact]
    public void TogglePick_HandlesNullTeam_Gracefully()
    {
        // Arrange
        var selectedPicks = new List<string> { "Team1" };
        var games = new List<Game>();

        // Act - Should not throw
        PickSelectionHelper.TogglePick(null!, selectedPicks, games);

        // Assert - No change
        selectedPicks.Should().ContainSingle().Which.Should().Be("Team1");
    }

    [Fact]
    public void TogglePick_HandlesEmptyTeam_Gracefully()
    {
        // Arrange
        var selectedPicks = new List<string> { "Team1" };
        var games = new List<Game>();

        // Act - Should not throw
        PickSelectionHelper.TogglePick("", selectedPicks, games);

        // Assert - No change
        selectedPicks.Should().ContainSingle().Which.Should().Be("Team1");
    }

    [Fact]
    public void TogglePick_HandlesNullSelectedPicks_Gracefully()
    {
        // Arrange
        var games = new List<Game>();

        // Act & Assert - Should not throw
        var act = () => PickSelectionHelper.TogglePick("Team", null!, games);
        act.Should().NotThrow();
    }

    [Fact]
    public void TogglePick_CustomMaxPicks_IsRespected()
    {
        // Arrange
        var selectedPicks = new List<string> { "Team1", "Team2", "Team3" };
        var games = new List<Game>
        {
            new Game { Favorite = "NewTeam", Underdog = "OtherTeam", Line = -5m }
        };

        // Act - Try to add 4th pick with maxPicks=3
        PickSelectionHelper.TogglePick("NewTeam", selectedPicks, games, maxPicks: 3);

        // Assert
        selectedPicks.Should().HaveCount(3);
        selectedPicks.Should().NotContain("NewTeam");
    }

    [Fact]
    public void TogglePick_AllowsAddingWithinLimit()
    {
        // Arrange
        var selectedPicks = new List<string> { "Team1", "Team2" };
        var games = new List<Game>
        {
            new Game { Favorite = "NewTeam", Underdog = "OtherTeam", Line = -5m }
        };

        // Act - Add 3rd pick with default maxPicks=6
        PickSelectionHelper.TogglePick("NewTeam", selectedPicks, games);

        // Assert
        selectedPicks.Should().HaveCount(3);
        selectedPicks.Should().Contain("NewTeam");
    }

    [Fact]
    public void TogglePick_RadioButtonBehavior_MultipleGames()
    {
        // Arrange - Multiple games, simulate picking from each
        var games = new List<Game>
        {
            new Game { Favorite = "Alabama", Underdog = "Georgia", Line = -7m },
            new Game { Favorite = "Michigan", Underdog = "Ohio State", Line = -3m },
            new Game { Favorite = "Texas", Underdog = "Oklahoma", Line = -5m }
        };
        var selectedPicks = new List<string>();

        // Act - Select from each game
        PickSelectionHelper.TogglePick("Alabama", selectedPicks, games);
        PickSelectionHelper.TogglePick("Ohio State", selectedPicks, games);
        PickSelectionHelper.TogglePick("Texas", selectedPicks, games);

        // Assert - 3 picks, one from each game
        selectedPicks.Should().HaveCount(3);
        selectedPicks.Should().Contain(new[] { "Alabama", "Ohio State", "Texas" });

        // Act - Switch to the opposite team in one game
        PickSelectionHelper.TogglePick("Georgia", selectedPicks, games);

        // Assert - Still 3 picks, but Georgia replaced Alabama
        selectedPicks.Should().HaveCount(3);
        selectedPicks.Should().Contain(new[] { "Georgia", "Ohio State", "Texas" });
        selectedPicks.Should().NotContain("Alabama");
    }

    [Fact]
    public void TogglePick_HandlesNullFavoriteOrUnderdog_Gracefully()
    {
        // Arrange - Game with null properties
        var selectedPicks = new List<string>();
        var games = new List<Game>
        {
            new Game { Favorite = null!, Underdog = "Georgia", Line = -7m },
            new Game { Favorite = "Michigan", Underdog = null!, Line = -3m },
            new Game { Favorite = "Texas", Underdog = "Oklahoma", Line = -5m }
        };

        // Act - Should not throw even with null properties
        PickSelectionHelper.TogglePick("Georgia", selectedPicks, games);
        PickSelectionHelper.TogglePick("Michigan", selectedPicks, games);
        PickSelectionHelper.TogglePick("Texas", selectedPicks, games);

        // Assert - Teams that could be found are added
        selectedPicks.Should().HaveCount(3);
        selectedPicks.Should().Contain(new[] { "Georgia", "Michigan", "Texas" });
    }

    [Fact]
    public void TogglePick_DeselectsTeam_WhenCalledWithDifferentCase()
    {
        // Arrange
        var selectedPicks = new List<string> { "alabama" };
        var games = new List<Game>
        {
            new Game { Favorite = "Alabama", Underdog = "Georgia", Line = -7m }
        };

        // Act - Try to deselect with different casing
        PickSelectionHelper.TogglePick("Alabama", selectedPicks, games);

        // Assert - Should be empty (deselected despite case difference)
        selectedPicks.Should().BeEmpty();
    }

    [Fact]
    public void TogglePick_AutoDeselectsOppositeTeam_WithMixedCase()
    {
        // Arrange - Opposite team stored with different case
        var selectedPicks = new List<string> { "GEORGIA" };
        var games = new List<Game>
        {
            new Game { Favorite = "Alabama", Underdog = "Georgia", Line = -7m }
        };

        // Act - Select Alabama, should deselect GEORGIA despite case mismatch
        PickSelectionHelper.TogglePick("Alabama", selectedPicks, games);

        // Assert - GEORGIA should be removed, Alabama added
        selectedPicks.Should().ContainSingle().Which.Should().Be("Alabama");
    }
}
