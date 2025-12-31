using AgainstTheSpread.Data.Entities;

namespace AgainstTheSpread.Data.Interfaces;

/// <summary>
/// Service for managing bowl games in the Against The Spread application.
/// </summary>
public interface IBowlGameService
{
    /// <summary>
    /// Synchronizes bowl games from lines data into the database.
    /// Creates new games or updates existing ones based on Year/GameNumber combination.
    /// </summary>
    /// <param name="year">The bowl season year.</param>
    /// <param name="games">The bowl games to sync.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of games synced (created or updated).</returns>
    Task<int> SyncBowlGamesAsync(
        int year,
        IEnumerable<BowlGameSyncInput> games,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all bowl games for a specific year.
    /// </summary>
    /// <param name="year">The bowl season year.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of bowl games for the specified year, ordered by game number.</returns>
    Task<List<BowlGameEntity>> GetBowlGamesAsync(int year, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific bowl game by ID.
    /// </summary>
    /// <param name="gameId">The bowl game ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The bowl game if found, null otherwise.</returns>
    Task<BowlGameEntity?> GetByIdAsync(int gameId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a specific bowl game is locked (kickoff time has passed).
    /// </summary>
    /// <param name="gameId">The bowl game ID to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if locked, false if unlocked, null if not found.</returns>
    Task<bool?> IsGameLockedAsync(int gameId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total number of bowl games for a year.
    /// </summary>
    /// <param name="year">The bowl season year.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Total game count.</returns>
    Task<int> GetTotalGamesCountAsync(int year, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enters a result for a single bowl game.
    /// </summary>
    /// <param name="gameId">The bowl game ID.</param>
    /// <param name="favoriteScore">The favorite's final score.</param>
    /// <param name="underdogScore">The underdog's final score.</param>
    /// <param name="enteredBy">The user ID of who entered the result.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated bowl game, or null if not found.</returns>
    Task<BowlGameEntity?> EnterResultAsync(
        int gameId,
        int favoriteScore,
        int underdogScore,
        Guid enteredBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Enters results for multiple bowl games.
    /// </summary>
    /// <param name="results">The results to enter.</param>
    /// <param name="enteredBy">The user ID of who entered the results.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing success/failure details.</returns>
    Task<BowlResultsEntryResult> BulkEnterResultsAsync(
        IEnumerable<BowlGameResultInput> results,
        Guid enteredBy,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Input model for syncing bowl games from lines data.
/// </summary>
public record BowlGameSyncInput(
    int GameNumber,
    string BowlName,
    string Favorite,
    string Underdog,
    decimal Line,
    DateTime GameDate);

/// <summary>
/// Input model for entering a bowl game result.
/// </summary>
public record BowlGameResultInput(int BowlGameId, int FavoriteScore, int UnderdogScore);

/// <summary>
/// Result of a bowl results entry attempt.
/// </summary>
public class BowlResultsEntryResult
{
    /// <summary>
    /// Whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Number of results successfully entered.
    /// </summary>
    public int ResultsEntered { get; set; }

    /// <summary>
    /// Number of results that failed.
    /// </summary>
    public int ResultsFailed { get; set; }

    /// <summary>
    /// Details about failed result entries.
    /// </summary>
    public List<FailedBowlResult> FailedResults { get; set; } = new();

    /// <summary>
    /// General error message if the entire operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static BowlResultsEntryResult CreateSuccess(int entered, List<FailedBowlResult>? failed = null)
    {
        return new BowlResultsEntryResult
        {
            Success = true,
            ResultsEntered = entered,
            ResultsFailed = failed?.Count ?? 0,
            FailedResults = failed ?? new()
        };
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static BowlResultsEntryResult CreateFailure(string error)
    {
        return new BowlResultsEntryResult
        {
            Success = false,
            ErrorMessage = error
        };
    }
}

/// <summary>
/// Details about a failed bowl result entry.
/// </summary>
public record FailedBowlResult(int BowlGameId, string Reason);
