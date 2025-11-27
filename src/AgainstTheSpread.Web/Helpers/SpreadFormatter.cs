using AgainstTheSpread.Core.Models;

namespace AgainstTheSpread.Web.Helpers;

/// <summary>
/// Helper class for formatting spread values for display.
/// </summary>
public static class SpreadFormatter
{
    /// <summary>
    /// Formats the spread for a given team based on whether they are the favorite or underdog.
    /// </summary>
    /// <param name="game">The game containing the spread information</param>
    /// <param name="teamName">The name of the team to format the spread for</param>
    /// <returns>Formatted spread string (e.g., "-6.5" for favorites, "+3" for underdogs)</returns>
    public static string FormatSpreadForTeam(Game game, string teamName)
    {
        if (game == null)
        {
            throw new ArgumentNullException(nameof(game));
        }

        if (string.IsNullOrEmpty(teamName))
        {
            throw new ArgumentException("Team name cannot be null or empty", nameof(teamName));
        }

        if (game.Favorite.Equals(teamName, StringComparison.OrdinalIgnoreCase))
        {
            // Team is the favorite, show negative spread
            return game.Line.ToString("0.#");
        }
        else
        {
            // Team is the underdog, show positive spread (opposite of the line)
            var underdogSpread = -game.Line;
            return underdogSpread > 0 ? $"+{underdogSpread:0.#}" : underdogSpread.ToString("0.#");
        }
    }
}
