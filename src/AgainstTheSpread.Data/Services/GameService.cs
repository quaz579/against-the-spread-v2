using AgainstTheSpread.Data.Entities;
using AgainstTheSpread.Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgainstTheSpread.Data.Services;

/// <summary>
/// Service for managing games in the Against The Spread application.
/// </summary>
public class GameService : IGameService
{
    private readonly AtsDbContext _context;
    private readonly ILogger<GameService> _logger;

    /// <summary>
    /// Initializes a new instance of GameService.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    public GameService(AtsDbContext context, ILogger<GameService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<int> SyncGamesFromLinesAsync(
        int year,
        int week,
        IEnumerable<GameSyncInput> games,
        CancellationToken cancellationToken = default)
    {
        var gamesList = games.ToList();
        if (!gamesList.Any())
        {
            _logger.LogInformation("No games to sync for year {Year} week {Week}", year, week);
            return 0;
        }

        // Get existing games for this week
        var existingGames = await _context.Games
            .Where(g => g.Year == year && g.Week == week)
            .ToListAsync(cancellationToken);

        var syncedCount = 0;

        foreach (var gameInput in gamesList)
        {
            // Try to find existing game by unique combination
            var existingGame = existingGames.FirstOrDefault(g =>
                g.Favorite == gameInput.Favorite && g.Underdog == gameInput.Underdog);

            if (existingGame != null)
            {
                // Update existing game
                existingGame.Line = gameInput.Line;
                existingGame.GameDate = gameInput.GameDate;
                _logger.LogDebug("Updated game {Favorite} vs {Underdog} for week {Week}",
                    gameInput.Favorite, gameInput.Underdog, week);
            }
            else
            {
                // Create new game
                var newGame = new GameEntity
                {
                    Year = year,
                    Week = week,
                    Favorite = gameInput.Favorite,
                    Underdog = gameInput.Underdog,
                    Line = gameInput.Line,
                    GameDate = gameInput.GameDate
                };
                _context.Games.Add(newGame);
                _logger.LogDebug("Created game {Favorite} vs {Underdog} for week {Week}",
                    gameInput.Favorite, gameInput.Underdog, week);
            }

            syncedCount++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Synced {Count} games for year {Year} week {Week}", syncedCount, year, week);

        return syncedCount;
    }

    /// <inheritdoc/>
    public async Task<List<GameEntity>> GetWeekGamesAsync(int year, int week, CancellationToken cancellationToken = default)
    {
        return await _context.Games
            .Where(g => g.Year == year && g.Week == week)
            .OrderBy(g => g.GameDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool?> IsGameLockedAsync(int gameId, CancellationToken cancellationToken = default)
    {
        var game = await _context.Games.FindAsync(new object[] { gameId }, cancellationToken);

        if (game == null)
        {
            _logger.LogWarning("Attempted to check lock status for non-existent game {GameId}", gameId);
            return null;
        }

        return game.IsLocked;
    }

    /// <inheritdoc/>
    public async Task<GameEntity?> GetByIdAsync(int gameId, CancellationToken cancellationToken = default)
    {
        return await _context.Games.FindAsync(new object[] { gameId }, cancellationToken);
    }
}
