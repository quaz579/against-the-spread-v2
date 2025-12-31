namespace AgainstTheSpread.Core.Interfaces;

/// <summary>
/// Interface for retrieving sports data from external providers.
/// </summary>
public interface ISportsDataProvider
{
    /// <summary>
    /// Gets the provider name (e.g., "CollegeFootballData").
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Gets games with betting lines for a specific week.
    /// </summary>
    /// <param name="year">The season year.</param>
    /// <param name="week">The week number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of games with lines.</returns>
    Task<List<ExternalGame>> GetWeeklyGamesAsync(int year, int week, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets bowl games with betting lines for a season.
    /// </summary>
    /// <param name="year">The bowl season year.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of bowl games with lines.</returns>
    Task<List<ExternalBowlGame>> GetBowlGamesAsync(int year, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the result for a specific game.
    /// </summary>
    /// <param name="gameId">The external game ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Game result if available, null otherwise.</returns>
    Task<ExternalGameResult?> GetGameResultAsync(string gameId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets results for all games in a week.
    /// </summary>
    /// <param name="year">The season year.</param>
    /// <param name="week">The week number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of game results.</returns>
    Task<List<ExternalGameResult>> GetWeeklyResultsAsync(int year, int week, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents an external game with betting line information.
/// </summary>
public class ExternalGame
{
    /// <summary>
    /// External provider's game ID.
    /// </summary>
    public string GameId { get; set; } = string.Empty;

    /// <summary>
    /// The favored team.
    /// </summary>
    public string Favorite { get; set; } = string.Empty;

    /// <summary>
    /// The underdog team.
    /// </summary>
    public string Underdog { get; set; } = string.Empty;

    /// <summary>
    /// The point spread (negative for favorite).
    /// </summary>
    public decimal Line { get; set; }

    /// <summary>
    /// Scheduled kickoff time.
    /// </summary>
    public DateTime GameDate { get; set; }

    /// <summary>
    /// Season year.
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Week number.
    /// </summary>
    public int Week { get; set; }

    /// <summary>
    /// Home team name.
    /// </summary>
    public string HomeTeam { get; set; } = string.Empty;

    /// <summary>
    /// Away team name.
    /// </summary>
    public string AwayTeam { get; set; } = string.Empty;

    /// <summary>
    /// Provider that provided the line.
    /// </summary>
    public string LineProvider { get; set; } = string.Empty;
}

/// <summary>
/// Represents an external bowl game with betting line information.
/// </summary>
public class ExternalBowlGame
{
    /// <summary>
    /// External provider's game ID.
    /// </summary>
    public string GameId { get; set; } = string.Empty;

    /// <summary>
    /// Bowl name (e.g., "Rose Bowl").
    /// </summary>
    public string BowlName { get; set; } = string.Empty;

    /// <summary>
    /// The favored team.
    /// </summary>
    public string Favorite { get; set; } = string.Empty;

    /// <summary>
    /// The underdog team.
    /// </summary>
    public string Underdog { get; set; } = string.Empty;

    /// <summary>
    /// The point spread (negative for favorite).
    /// </summary>
    public decimal Line { get; set; }

    /// <summary>
    /// Scheduled kickoff time.
    /// </summary>
    public DateTime GameDate { get; set; }

    /// <summary>
    /// Bowl season year.
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Home team name.
    /// </summary>
    public string HomeTeam { get; set; } = string.Empty;

    /// <summary>
    /// Away team name.
    /// </summary>
    public string AwayTeam { get; set; } = string.Empty;

    /// <summary>
    /// Provider that provided the line.
    /// </summary>
    public string LineProvider { get; set; } = string.Empty;
}

/// <summary>
/// Represents a game result from an external provider.
/// </summary>
public class ExternalGameResult
{
    /// <summary>
    /// External provider's game ID.
    /// </summary>
    public string GameId { get; set; } = string.Empty;

    /// <summary>
    /// Home team name.
    /// </summary>
    public string HomeTeam { get; set; } = string.Empty;

    /// <summary>
    /// Away team name.
    /// </summary>
    public string AwayTeam { get; set; } = string.Empty;

    /// <summary>
    /// Home team score.
    /// </summary>
    public int HomeScore { get; set; }

    /// <summary>
    /// Away team score.
    /// </summary>
    public int AwayScore { get; set; }

    /// <summary>
    /// Whether the game is completed.
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// Season year.
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Week number (null for bowl games).
    /// </summary>
    public int? Week { get; set; }
}
