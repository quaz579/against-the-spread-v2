namespace AgainstTheSpread.Core.Models;

/// <summary>
/// Represents a single bowl game with betting line information.
/// Bowl games differ from regular season games by having a bowl name.
/// </summary>
public class BowlGame
{
    /// <summary>
    /// Name of the bowl game (e.g., "Rose Bowl", "Sugar Bowl")
    /// </summary>
    public string BowlName { get; set; } = string.Empty;

    /// <summary>
    /// Game sequence number (1-36, determines order on template)
    /// </summary>
    public int GameNumber { get; set; }

    /// <summary>
    /// The favored team name
    /// </summary>
    public string Favorite { get; set; } = string.Empty;

    /// <summary>
    /// The point spread (negative number indicating favorite margin)
    /// </summary>
    public decimal Line { get; set; }

    /// <summary>
    /// The underdog team name
    /// </summary>
    public string Underdog { get; set; } = string.Empty;

    /// <summary>
    /// Date and time when the game is scheduled
    /// </summary>
    public DateTime GameDate { get; set; }

    /// <summary>
    /// Display string for the favorite with line (e.g., "Alabama -9.5")
    /// </summary>
    public string FavoriteDisplay => $"{Favorite} {Line}";

    /// <summary>
    /// Full game description
    /// </summary>
    public string GameDescription => $"{BowlName}: {Favorite} {Line} vs {Underdog}";
}
