namespace AgainstTheSpread.Data.Interfaces;

/// <summary>
/// Service for calculating and retrieving leaderboard standings.
/// </summary>
public interface ILeaderboardService
{
    /// <summary>
    /// Get the weekly leaderboard for a specific week.
    /// </summary>
    /// <param name="year">The season year.</param>
    /// <param name="week">The week number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of weekly leaderboard entries sorted by wins.</returns>
    Task<List<WeeklyLeaderboardEntry>> GetWeeklyLeaderboardAsync(
        int year,
        int week,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the season leaderboard aggregating all weeks.
    /// </summary>
    /// <param name="year">The season year.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of season leaderboard entries sorted by total wins.</returns>
    Task<List<SeasonLeaderboardEntry>> GetSeasonLeaderboardAsync(
        int year,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a user's detailed pick history for a season.
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <param name="year">The season year.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>User's season pick history.</returns>
    Task<UserSeasonHistory?> GetUserSeasonHistoryAsync(
        Guid userId,
        int year,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Weekly leaderboard entry showing a user's performance for a single week.
/// </summary>
public record WeeklyLeaderboardEntry(
    Guid UserId,
    string DisplayName,
    int Week,
    decimal Wins,
    decimal Losses,
    int Pushes,
    decimal WinPercentage);

/// <summary>
/// Season leaderboard entry showing a user's cumulative performance.
/// </summary>
public record SeasonLeaderboardEntry(
    Guid UserId,
    string DisplayName,
    decimal TotalWins,
    decimal TotalLosses,
    int TotalPushes,
    decimal WinPercentage,
    int WeeksPlayed,
    int PerfectWeeks);

/// <summary>
/// User's season pick history.
/// </summary>
public class UserSeasonHistory
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal TotalWins { get; set; }
    public decimal TotalLosses { get; set; }
    public int TotalPushes { get; set; }
    public decimal WinPercentage { get; set; }
    public List<UserWeekHistory> Weeks { get; set; } = new();
}

/// <summary>
/// User's picks and results for a single week.
/// </summary>
public class UserWeekHistory
{
    public int Week { get; set; }
    public decimal Wins { get; set; }
    public decimal Losses { get; set; }
    public int Pushes { get; set; }
    public bool IsPerfect { get; set; }
    public List<UserPickResult> Picks { get; set; } = new();
}

/// <summary>
/// Result of a single user pick.
/// </summary>
public class UserPickResult
{
    public int GameId { get; set; }
    public string Favorite { get; set; } = string.Empty;
    public string Underdog { get; set; } = string.Empty;
    public decimal Line { get; set; }
    public string SelectedTeam { get; set; } = string.Empty;
    public string? SpreadWinner { get; set; }
    public bool? IsPush { get; set; }
    public bool? IsWin { get; set; }
    public bool HasResult { get; set; }
}
