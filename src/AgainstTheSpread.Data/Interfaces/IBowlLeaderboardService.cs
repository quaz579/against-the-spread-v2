namespace AgainstTheSpread.Data.Interfaces;

/// <summary>
/// Service for calculating bowl game leaderboard standings.
/// Bowl scoring is based on confidence points - you earn the confidence points
/// you assigned to games where your pick was correct.
/// </summary>
public interface IBowlLeaderboardService
{
    /// <summary>
    /// Gets the bowl leaderboard for a specific year.
    /// </summary>
    /// <param name="year">The bowl season year.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of leaderboard entries sorted by total points descending.</returns>
    Task<List<BowlLeaderboardEntry>> GetBowlLeaderboardAsync(
        int year,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific user's bowl picks history with results.
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <param name="year">The bowl season year.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>User's bowl picks with scoring details.</returns>
    Task<BowlUserHistory?> GetUserBowlHistoryAsync(
        Guid userId,
        int year,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// A single entry in the bowl leaderboard.
/// </summary>
public class BowlLeaderboardEntry
{
    /// <summary>
    /// The user's ID.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The user's display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Total confidence points earned from correct spread picks.
    /// </summary>
    public int SpreadPoints { get; set; }

    /// <summary>
    /// Total correct spread picks.
    /// </summary>
    public int SpreadWins { get; set; }

    /// <summary>
    /// Total incorrect spread picks.
    /// </summary>
    public int SpreadLosses { get; set; }

    /// <summary>
    /// Total push results for spread picks.
    /// </summary>
    public int SpreadPushes { get; set; }

    /// <summary>
    /// Total correct outright winner picks.
    /// </summary>
    public int OutrightWins { get; set; }

    /// <summary>
    /// Total possible points (sum of all confidence values).
    /// </summary>
    public int MaxPossiblePoints { get; set; }

    /// <summary>
    /// Percentage of possible points earned.
    /// </summary>
    public decimal PointsPercentage => MaxPossiblePoints > 0
        ? Math.Round((decimal)SpreadPoints / MaxPossiblePoints * 100, 1)
        : 0;

    /// <summary>
    /// Number of games with results.
    /// </summary>
    public int GamesCompleted { get; set; }

    /// <summary>
    /// Total games in the bowl season.
    /// </summary>
    public int TotalGames { get; set; }
}

/// <summary>
/// User's bowl picks history with detailed results.
/// </summary>
public class BowlUserHistory
{
    /// <summary>
    /// The user's ID.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The user's display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// The bowl season year.
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Total confidence points earned.
    /// </summary>
    public int TotalPoints { get; set; }

    /// <summary>
    /// Maximum possible points.
    /// </summary>
    public int MaxPossiblePoints { get; set; }

    /// <summary>
    /// Individual pick details.
    /// </summary>
    public List<BowlPickDetail> Picks { get; set; } = new();
}

/// <summary>
/// Detail of a single bowl pick with result.
/// </summary>
public class BowlPickDetail
{
    /// <summary>
    /// Game number.
    /// </summary>
    public int GameNumber { get; set; }

    /// <summary>
    /// Bowl name.
    /// </summary>
    public string BowlName { get; set; } = string.Empty;

    /// <summary>
    /// Favorite team.
    /// </summary>
    public string Favorite { get; set; } = string.Empty;

    /// <summary>
    /// Underdog team.
    /// </summary>
    public string Underdog { get; set; } = string.Empty;

    /// <summary>
    /// The point spread line.
    /// </summary>
    public decimal Line { get; set; }

    /// <summary>
    /// User's spread pick.
    /// </summary>
    public string SpreadPick { get; set; } = string.Empty;

    /// <summary>
    /// Confidence points assigned.
    /// </summary>
    public int ConfidencePoints { get; set; }

    /// <summary>
    /// User's outright winner pick.
    /// </summary>
    public string OutrightWinnerPick { get; set; } = string.Empty;

    /// <summary>
    /// Whether the game has a result.
    /// </summary>
    public bool HasResult { get; set; }

    /// <summary>
    /// Favorite's final score.
    /// </summary>
    public int? FavoriteScore { get; set; }

    /// <summary>
    /// Underdog's final score.
    /// </summary>
    public int? UnderdogScore { get; set; }

    /// <summary>
    /// Who covered the spread (null if push or no result).
    /// </summary>
    public string? SpreadWinner { get; set; }

    /// <summary>
    /// Whether the result was a push.
    /// </summary>
    public bool? IsPush { get; set; }

    /// <summary>
    /// Who won the game outright.
    /// </summary>
    public string? ActualOutrightWinner { get; set; }

    /// <summary>
    /// Points earned for this pick (confidence points if correct, 0 if not).
    /// </summary>
    public int PointsEarned { get; set; }

    /// <summary>
    /// Whether the spread pick was correct.
    /// </summary>
    public bool? SpreadPickCorrect { get; set; }

    /// <summary>
    /// Whether the outright winner pick was correct.
    /// </summary>
    public bool? OutrightPickCorrect { get; set; }
}
