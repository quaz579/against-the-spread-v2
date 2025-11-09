namespace AgainstTheSpread.Core.Models;

/// <summary>
/// Represents the weekly betting lines for all games
/// </summary>
public class WeeklyLines
{
    /// <summary>
    /// The week number (1-14 for regular season)
    /// </summary>
    public int Week { get; set; }

    /// <summary>
    /// List of all games for this week
    /// </summary>
    public List<Game> Games { get; set; } = new();

    /// <summary>
    /// When the lines were uploaded by admin
    /// </summary>
    public DateTime UploadedAt { get; set; }

    /// <summary>
    /// Year of the season (e.g., 2025)
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Returns total number of games available for this week
    /// </summary>
    public int TotalGames => Games.Count;

    /// <summary>
    /// Validates that the weekly lines data is complete
    /// </summary>
    public bool IsValid() => Week > 0 && Week <= 14 && Games != null && Games.Any() && Year >= 2020;
}
