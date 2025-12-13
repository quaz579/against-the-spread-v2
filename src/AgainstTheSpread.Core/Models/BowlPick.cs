namespace AgainstTheSpread.Core.Models;

/// <summary>
/// Represents a user's pick for a single bowl game.
/// Each bowl pick includes the spread pick, confidence points, and outright winner.
/// </summary>
public class BowlPick
{
    /// <summary>
    /// The game number this pick is for (1 through total game count)
    /// </summary>
    public int GameNumber { get; set; }

    /// <summary>
    /// The team picked against the spread
    /// </summary>
    public string SpreadPick { get; set; } = string.Empty;

    /// <summary>
    /// Confidence points assigned to this pick (1 through total game count, unique per game)
    /// Higher = more confident
    /// </summary>
    public int ConfidencePoints { get; set; }

    /// <summary>
    /// The team picked to win outright (regardless of spread)
    /// </summary>
    public string OutrightWinner { get; set; } = string.Empty;

    /// <summary>
    /// Validates that this pick is complete for a given total game count
    /// </summary>
    public bool IsValid(int totalGames = 36)
    {
        return GameNumber >= 1 
            && GameNumber <= totalGames
            && !string.IsNullOrWhiteSpace(SpreadPick)
            && ConfidencePoints >= 1 
            && ConfidencePoints <= totalGames
            && !string.IsNullOrWhiteSpace(OutrightWinner);
    }
}
