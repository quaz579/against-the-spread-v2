using AgainstTheSpread.Data.Entities;

namespace AgainstTheSpread.Data.Interfaces;

/// <summary>
/// Service for managing games in the Against The Spread application.
/// </summary>
public interface IGameService
{
    /// <summary>
    /// Synchronizes games from weekly lines data into the database.
    /// Creates new games or updates existing ones based on Year/Week/Favorite/Underdog combination.
    /// </summary>
    /// <param name="year">The season year.</param>
    /// <param name="week">The week number.</param>
    /// <param name="games">The games to sync.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of games synced (created or updated).</returns>
    Task<int> SyncGamesFromLinesAsync(
        int year,
        int week,
        IEnumerable<GameSyncInput> games,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all games for a specific week.
    /// </summary>
    /// <param name="year">The season year.</param>
    /// <param name="week">The week number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of games for the specified week.</returns>
    Task<List<GameEntity>> GetWeekGamesAsync(int year, int week, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a specific game is locked (kickoff time has passed).
    /// </summary>
    /// <param name="gameId">The game ID to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the game is locked, false if unlocked. Null if game not found.</returns>
    Task<bool?> IsGameLockedAsync(int gameId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific game by ID.
    /// </summary>
    /// <param name="gameId">The game ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The game if found, null otherwise.</returns>
    Task<GameEntity?> GetByIdAsync(int gameId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets list of weeks that have games for a specific year.
    /// </summary>
    /// <param name="year">The season year.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of week numbers that have games.</returns>
    Task<List<int>> GetAvailableWeeksAsync(int year, CancellationToken cancellationToken = default);
}

/// <summary>
/// Input model for syncing games from weekly lines.
/// </summary>
public record GameSyncInput(
    string Favorite,
    string Underdog,
    decimal Line,
    DateTime GameDate);
