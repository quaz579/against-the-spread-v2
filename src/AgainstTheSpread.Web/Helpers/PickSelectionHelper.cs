using AgainstTheSpread.Core.Models;

namespace AgainstTheSpread.Web.Helpers;

/// <summary>
/// Helper class for managing team pick selection with radio-button behavior per game.
/// </summary>
public static class PickSelectionHelper
{
    /// <summary>
    /// Toggles a team pick, ensuring only one team from each game can be selected.
    /// When selecting a team, automatically deselects the opposite team from the same game.
    /// </summary>
    /// <param name="team">The team to toggle</param>
    /// <param name="selectedPicks">Current list of selected picks</param>
    /// <param name="games">Available games to find team pairings</param>
    /// <param name="maxPicks">Maximum number of picks allowed</param>
    public static void TogglePick(string team, List<string> selectedPicks, IEnumerable<Game>? games, int maxPicks = 6)
    {
        if (string.IsNullOrEmpty(team) || selectedPicks == null)
            return;

        if (selectedPicks.Contains(team))
        {
            selectedPicks.Remove(team);
            return;
        }

        // Find the game this team belongs to
        var game = games?.FirstOrDefault(g =>
            g.Favorite.Equals(team, StringComparison.OrdinalIgnoreCase) ||
            g.Underdog.Equals(team, StringComparison.OrdinalIgnoreCase));

        // Get the opposite team from this game
        string? oppositeTeam = null;
        if (game != null)
        {
            oppositeTeam = game.Favorite.Equals(team, StringComparison.OrdinalIgnoreCase)
                ? game.Underdog
                : game.Favorite;
        }

        // Check if selecting this team would require deselecting the opposite team
        bool oppositeTeamSelected = oppositeTeam != null && selectedPicks.Contains(oppositeTeam);

        // If opposite team is selected, we can switch (radio button behavior)
        // If opposite team is not selected, we need room for a new pick
        if (oppositeTeamSelected || selectedPicks.Count < maxPicks)
        {
            // Remove the opposite team if it's selected
            if (oppositeTeamSelected && oppositeTeam != null)
            {
                selectedPicks.Remove(oppositeTeam);
            }

            selectedPicks.Add(team);
        }
    }
}
