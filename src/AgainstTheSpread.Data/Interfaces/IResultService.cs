using AgainstTheSpread.Data.Entities;

namespace AgainstTheSpread.Data.Interfaces;

/// <summary>
/// Service for managing game results and spread calculations.
/// </summary>
public interface IResultService
{
    /// <summary>
    /// Enter the result for a single game.
    /// </summary>
    /// <param name="gameId">The database ID of the game.</param>
    /// <param name="favoriteScore">Score of the favorite team.</param>
    /// <param name="underdogScore">Score of the underdog team.</param>
    /// <param name="adminUserId">The ID of the admin entering the result.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated game entity, or null if game not found.</returns>
    Task<GameEntity?> EnterResultAsync(
        int gameId,
        int favoriteScore,
        int underdogScore,
        Guid adminUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Enter results for multiple games in a week.
    /// </summary>
    /// <param name="year">The season year.</param>
    /// <param name="week">The week number.</param>
    /// <param name="results">List of game results to enter.</param>
    /// <param name="adminUserId">The ID of the admin entering the results.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing success/failure information for each game.</returns>
    Task<BulkResultEntryResult> BulkEnterResultsAsync(
        int year,
        int week,
        IEnumerable<GameResultInput> results,
        Guid adminUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get results for a specific week.
    /// </summary>
    /// <param name="year">The season year.</param>
    /// <param name="week">The week number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of games with their results (if entered).</returns>
    Task<List<GameEntity>> GetWeekResultsAsync(
        int year,
        int week,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate the spread winner based on scores.
    /// </summary>
    /// <param name="favorite">The favorite team name.</param>
    /// <param name="underdog">The underdog team name.</param>
    /// <param name="line">The point spread (negative for favorite).</param>
    /// <param name="favoriteScore">The favorite's score.</param>
    /// <param name="underdogScore">The underdog's score.</param>
    /// <returns>Tuple containing winner (or null for push) and isPush flag.</returns>
    (string? Winner, bool IsPush) CalculateSpreadWinner(
        string favorite,
        string underdog,
        decimal line,
        int favoriteScore,
        int underdogScore);
}

/// <summary>
/// Input model for entering a game result.
/// </summary>
public record GameResultInput(int GameId, int FavoriteScore, int UnderdogScore);

/// <summary>
/// Result of a bulk result entry operation.
/// </summary>
public class BulkResultEntryResult
{
    public bool Success { get; set; }
    public int ResultsEntered { get; set; }
    public int ResultsFailed { get; set; }
    public List<FailedResult> FailedResults { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public static BulkResultEntryResult CreateSuccess(int entered) =>
        new() { Success = true, ResultsEntered = entered };

    public static BulkResultEntryResult CreatePartialSuccess(int entered, List<FailedResult> failed) =>
        new() { Success = true, ResultsEntered = entered, ResultsFailed = failed.Count, FailedResults = failed };

    public static BulkResultEntryResult CreateFailure(string errorMessage) =>
        new() { Success = false, ErrorMessage = errorMessage };
}

/// <summary>
/// Information about a failed result entry.
/// </summary>
public record FailedResult(int GameId, string Reason);
