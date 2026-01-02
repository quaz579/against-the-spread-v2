using AgainstTheSpread.Data.Entities;
using AgainstTheSpread.Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgainstTheSpread.Data.Services;

/// <summary>
/// Service for managing bowl games in the Against The Spread application.
/// </summary>
public class BowlGameService : IBowlGameService
{
    private readonly AtsDbContext _context;
    private readonly ILogger<BowlGameService> _logger;
    private readonly ITeamNameNormalizer? _teamNameNormalizer;

    /// <summary>
    /// Initializes a new instance of BowlGameService.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    /// <param name="teamNameNormalizer">Optional team name normalizer for consistent naming.</param>
    public BowlGameService(
        AtsDbContext context,
        ILogger<BowlGameService> logger,
        ITeamNameNormalizer? teamNameNormalizer = null)
    {
        _context = context;
        _logger = logger;
        _teamNameNormalizer = teamNameNormalizer;
    }

    /// <inheritdoc/>
    public async Task<int> SyncBowlGamesAsync(
        int year,
        IEnumerable<BowlGameSyncInput> games,
        CancellationToken cancellationToken = default)
    {
        var gamesList = games.ToList();
        if (!gamesList.Any())
        {
            _logger.LogInformation("No bowl games to sync for year {Year}", year);
            return 0;
        }

        var syncedCount = 0;
        var processedMatchups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Get existing games for this year
        var existingGames = await _context.BowlGames
            .Where(g => g.Year == year)
            .ToDictionaryAsync(g => g.GameNumber, cancellationToken);

        foreach (var input in gamesList)
        {
            // Normalize team names if normalizer is available
            var favorite = input.Favorite;
            var underdog = input.Underdog;

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
                    "Duplicate bowl game detected: {Favorite} vs {Underdog} (original: {OrigFav} vs {OrigUnd}). Skipping.",
                    favorite, underdog, input.Favorite, input.Underdog);
                continue;
            }
            processedMatchups.Add(matchupKey);

            if (existingGames.TryGetValue(input.GameNumber, out var existingGame))
            {
                // Update existing game
                existingGame.BowlName = input.BowlName;
                existingGame.Favorite = favorite;
                existingGame.Underdog = underdog;
                existingGame.Line = input.Line;
                existingGame.GameDate = input.GameDate;
                _logger.LogDebug("Updated bowl game {GameNumber} for year {Year}", input.GameNumber, year);
            }
            else
            {
                // Create new game
                var newGame = new BowlGameEntity
                {
                    Year = year,
                    GameNumber = input.GameNumber,
                    BowlName = input.BowlName,
                    Favorite = favorite,
                    Underdog = underdog,
                    Line = input.Line,
                    GameDate = input.GameDate
                };
                _context.BowlGames.Add(newGame);
                _logger.LogDebug("Created bowl game {GameNumber} for year {Year}", input.GameNumber, year);
            }

            syncedCount++;
        }

        if (syncedCount > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Synced {Count} bowl games for year {Year}", syncedCount, year);
        return syncedCount;
    }

    /// <inheritdoc/>
    public async Task<List<BowlGameEntity>> GetBowlGamesAsync(int year, CancellationToken cancellationToken = default)
    {
        return await _context.BowlGames
            .Where(g => g.Year == year)
            .OrderBy(g => g.GameNumber)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<BowlGameEntity?> GetByIdAsync(int gameId, CancellationToken cancellationToken = default)
    {
        return await _context.BowlGames
            .FirstOrDefaultAsync(g => g.Id == gameId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool?> IsGameLockedAsync(int gameId, CancellationToken cancellationToken = default)
    {
        var game = await _context.BowlGames
            .FirstOrDefaultAsync(g => g.Id == gameId, cancellationToken);

        if (game == null)
        {
            return null;
        }

        return game.IsLocked;
    }

    /// <inheritdoc/>
    public async Task<int> GetTotalGamesCountAsync(int year, CancellationToken cancellationToken = default)
    {
        return await _context.BowlGames
            .CountAsync(g => g.Year == year, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> BowlGamesExistAsync(int year, CancellationToken cancellationToken = default)
    {
        return await _context.BowlGames.AnyAsync(g => g.Year == year, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<BowlGameEntity?> EnterResultAsync(
        int gameId,
        int favoriteScore,
        int underdogScore,
        Guid enteredBy,
        CancellationToken cancellationToken = default)
    {
        var game = await _context.BowlGames
            .FirstOrDefaultAsync(g => g.Id == gameId, cancellationToken);

        if (game == null)
        {
            _logger.LogWarning("Bowl game {GameId} not found", gameId);
            return null;
        }

        // Calculate result
        game.FavoriteScore = favoriteScore;
        game.UnderdogScore = underdogScore;

        // Determine spread winner
        // The spread represents how much the favorite is expected to win by
        // If favorite wins by more than the spread, they cover
        // If underdog loses by less than the spread (or wins), they cover
        var favoriteMargin = favoriteScore - underdogScore;
        var spreadMargin = favoriteMargin + game.Line; // Line is typically negative

        if (spreadMargin > 0)
        {
            game.SpreadWinner = game.Favorite;
            game.IsPush = false;
        }
        else if (spreadMargin < 0)
        {
            game.SpreadWinner = game.Underdog;
            game.IsPush = false;
        }
        else
        {
            game.SpreadWinner = null;
            game.IsPush = true;
        }

        // Determine outright winner
        if (favoriteScore > underdogScore)
        {
            game.OutrightWinner = game.Favorite;
        }
        else if (underdogScore > favoriteScore)
        {
            game.OutrightWinner = game.Underdog;
        }
        else
        {
            game.OutrightWinner = null; // Tie (rare in football)
        }

        game.ResultEnteredAt = DateTime.UtcNow;
        game.ResultEnteredBy = enteredBy;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Entered result for bowl game {GameId}: {Favorite} {FavScore} - {Underdog} {UndScore}",
            gameId, game.Favorite, favoriteScore, game.Underdog, underdogScore);

        return game;
    }

    /// <inheritdoc/>
    public async Task<BowlResultsEntryResult> BulkEnterResultsAsync(
        IEnumerable<BowlGameResultInput> results,
        Guid enteredBy,
        CancellationToken cancellationToken = default)
    {
        var resultsList = results.ToList();
        if (!resultsList.Any())
        {
            return BowlResultsEntryResult.CreateSuccess(0);
        }

        var enteredCount = 0;
        var failedResults = new List<FailedBowlResult>();

        // Get all games in one query
        var gameIds = resultsList.Select(r => r.BowlGameId).ToList();
        var games = await _context.BowlGames
            .Where(g => gameIds.Contains(g.Id))
            .ToDictionaryAsync(g => g.Id, cancellationToken);

        foreach (var result in resultsList)
        {
            if (!games.TryGetValue(result.BowlGameId, out var game))
            {
                failedResults.Add(new FailedBowlResult(result.BowlGameId, "Bowl game not found"));
                continue;
            }

            // Calculate result
            game.FavoriteScore = result.FavoriteScore;
            game.UnderdogScore = result.UnderdogScore;

            // Determine spread winner
            var favoriteMargin = result.FavoriteScore - result.UnderdogScore;
            var spreadMargin = favoriteMargin + game.Line;

            if (spreadMargin > 0)
            {
                game.SpreadWinner = game.Favorite;
                game.IsPush = false;
            }
            else if (spreadMargin < 0)
            {
                game.SpreadWinner = game.Underdog;
                game.IsPush = false;
            }
            else
            {
                game.SpreadWinner = null;
                game.IsPush = true;
            }

            // Determine outright winner
            if (result.FavoriteScore > result.UnderdogScore)
            {
                game.OutrightWinner = game.Favorite;
            }
            else if (result.UnderdogScore > result.FavoriteScore)
            {
                game.OutrightWinner = game.Underdog;
            }
            else
            {
                game.OutrightWinner = null;
            }

            game.ResultEnteredAt = DateTime.UtcNow;
            game.ResultEnteredBy = enteredBy;
            enteredCount++;
        }

        if (enteredCount > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Bulk entered {Count} bowl game results, {Failed} failed",
            enteredCount, failedResults.Count);

        return BowlResultsEntryResult.CreateSuccess(enteredCount, failedResults.Count > 0 ? failedResults : null);
    }
}
