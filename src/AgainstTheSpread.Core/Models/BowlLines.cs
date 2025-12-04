namespace AgainstTheSpread.Core.Models;

/// <summary>
/// Represents all bowl game lines for a season.
/// </summary>
public class BowlLines
{
    /// <summary>
    /// Year of the bowl season
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// List of all bowl games
    /// </summary>
    public List<BowlGame> Games { get; set; } = new();

    /// <summary>
    /// When the lines were uploaded by admin
    /// </summary>
    public DateTime UploadedAt { get; set; }

    /// <summary>
    /// Total number of games
    /// </summary>
    public int TotalGames => Games.Count;

    /// <summary>
    /// Validates that the bowl lines data is complete
    /// </summary>
    public bool IsValid() => Games != null && Games.Any() && Year >= 2020;
}
