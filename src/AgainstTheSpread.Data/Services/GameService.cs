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
    private readonly ITeamNameNormalizer? _teamNameNormalizer;

    /// <summary>
    /// Initializes a new instance of GameService.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    /// <param name="teamNameNormalizer">Optional team name normalizer for consistent naming.</param>
    public GameService(
        AtsDbContext context,
        ILogger<GameService> logger,
        ITeamNameNormalizer? teamNameNormalizer = null)
    {
        _context = context;
        _logger = logger;
        _teamNameNormalizer = teamNameNormalizer;
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
        var processedMatchups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var gameInput in gamesList)
        {
            // Normalize team names if normalizer is available
            var favorite = gameInput.Favorite;
            var underdog = gameInput.Underdog;

            if (_teamNameNormalizer != null)
            {
                favorite = await _teamNameNormalizer.NormalizeAsync(favorite, cancellationToken);
                underdog = await _teamNameNormalizer.NormalizeAsync(underdog, cancellationToken);
            }

            // Create a matchup key to detect duplicates (order-independent)
            var matchupKey = string.Compare(favorite, underdog, StringComparison.OrdinalIgnoreCase) < 0
                ? $"{favorite}|{underdog}"
                : $"{underdog}|{favorite}";

            if (processedMatchups.Contains(matchupKey))
            {
                _logger.LogWarning(
                    "Duplicate game detected in sync batch: {Favorite} vs {Underdog} (original: {OrigFav} vs {OrigUnd}). Skipping.",
                    favorite, underdog, gameInput.Favorite, gameInput.Underdog);
                continue;
            }
            processedMatchups.Add(matchupKey);

            // Try to find existing game by unique combination (check both team orderings)
            var existingGame = existingGames.FirstOrDefault(g =>
                (g.Favorite == favorite && g.Underdog == underdog) ||
                (g.Favorite == underdog && g.Underdog == favorite));

            if (existingGame != null)
            {
                // Update existing game
                existingGame.Favorite = favorite;
                existingGame.Underdog = underdog;
                existingGame.Line = gameInput.Line;
                existingGame.GameDate = gameInput.GameDate;
                _logger.LogDebug("Updated game {Favorite} vs {Underdog} for week {Week}",
                    favorite, underdog, week);
            }
            else
            {
                // Create new game
                var newGame = new GameEntity
                {
                    Year = year,
                    Week = week,
                    Favorite = favorite,
                    Underdog = underdog,
                    Line = gameInput.Line,
                    GameDate = gameInput.GameDate
                };
                _context.Games.Add(newGame);
                _logger.LogDebug("Created game {Favorite} vs {Underdog} for week {Week}",
                    favorite, underdog, week);
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

    /// <inheritdoc/>
    public async Task<List<int>> GetAvailableWeeksAsync(int year, CancellationToken cancellationToken = default)
    {
        return await _context.Games
            .Where(g => g.Year == year)
            .Select(g => g.Week)
            .Distinct()
            .OrderBy(w => w)
            .ToListAsync(cancellationToken);
    }
}
