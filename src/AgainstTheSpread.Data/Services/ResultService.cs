using AgainstTheSpread.Data.Entities;
using AgainstTheSpread.Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgainstTheSpread.Data.Services;

/// <summary>
/// Service for managing game results and spread calculations.
/// </summary>
public class ResultService : IResultService
{
    private readonly AtsDbContext _context;
    private readonly ILogger<ResultService> _logger;

    public ResultService(AtsDbContext context, ILogger<ResultService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<GameEntity?> EnterResultAsync(
        int gameId,
        int favoriteScore,
        int underdogScore,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        var game = await _context.Games.FindAsync(new object[] { gameId }, cancellationToken);
        if (game == null)
        {
            _logger.LogWarning("Game {GameId} not found for result entry", gameId);
            return null;
        }

        // Calculate spread winner
        var (winner, isPush) = CalculateSpreadWinner(
            game.Favorite,
            game.Underdog,
            game.Line,
            favoriteScore,
            underdogScore);

        // Update game with result
        game.FavoriteScore = favoriteScore;
        game.UnderdogScore = underdogScore;
        game.SpreadWinner = winner;
        game.IsPush = isPush;
        game.ResultEnteredAt = DateTime.UtcNow;
        game.ResultEnteredBy = adminUserId;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Result entered for game {GameId}: {Favorite} {FavoriteScore} - {Underdog} {UnderdogScore}, Spread Winner: {Winner}, IsPush: {IsPush}",
            gameId, game.Favorite, favoriteScore, game.Underdog, underdogScore, winner ?? "N/A", isPush);

        return game;
    }

    /// <inheritdoc/>
    public async Task<BulkResultEntryResult> BulkEnterResultsAsync(
        int year,
        int week,
        IEnumerable<GameResultInput> results,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        var resultsList = results.ToList();
        if (!resultsList.Any())
        {
            _logger.LogInformation("No results to enter for year {Year} week {Week}", year, week);
            return BulkResultEntryResult.CreateSuccess(0);
        }

        var entered = 0;
        var failed = new List<FailedResult>();

        // Get all games for this week
        var gameIds = resultsList.Select(r => r.GameId).ToList();
        var games = await _context.Games
            .Where(g => gameIds.Contains(g.Id) && g.Year == year && g.Week == week)
            .ToDictionaryAsync(g => g.Id, cancellationToken);

        foreach (var result in resultsList)
        {
            // Validate game exists and is for the correct week
            if (!games.TryGetValue(result.GameId, out var game))
            {
                failed.Add(new FailedResult(result.GameId, "Game not found or not for this week"));
                _logger.LogWarning("Game {GameId} not found for week {Week} during bulk result entry", result.GameId, week);
                continue;
            }

            // Validate scores are non-negative
            if (result.FavoriteScore < 0 || result.UnderdogScore < 0)
            {
                failed.Add(new FailedResult(result.GameId, "Scores cannot be negative"));
                continue;
            }

            // Calculate spread winner
            var (winner, isPush) = CalculateSpreadWinner(
                game.Favorite,
                game.Underdog,
                game.Line,
                result.FavoriteScore,
                result.UnderdogScore);

            // Update game with result
            game.FavoriteScore = result.FavoriteScore;
            game.UnderdogScore = result.UnderdogScore;
            game.SpreadWinner = winner;
            game.IsPush = isPush;
            game.ResultEnteredAt = DateTime.UtcNow;
            game.ResultEnteredBy = adminUserId;

            entered++;
        }

        if (entered > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation(
            "Bulk result entry for year {Year} week {Week}: {Entered} entered, {Failed} failed",
            year, week, entered, failed.Count);

        return failed.Any()
            ? BulkResultEntryResult.CreatePartialSuccess(entered, failed)
            : BulkResultEntryResult.CreateSuccess(entered);
    }

    /// <inheritdoc/>
    public async Task<List<GameEntity>> GetWeekResultsAsync(
        int year,
        int week,
        CancellationToken cancellationToken = default)
    {
        return await _context.Games
            .Where(g => g.Year == year && g.Week == week)
            .OrderBy(g => g.GameDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public (string? Winner, bool IsPush) CalculateSpreadWinner(
        string favorite,
        string underdog,
        decimal line,
        int favoriteScore,
        int underdogScore)
    {
        // Line is negative for favorite (e.g., -7.5)
        // Adjusted score = favorite score + line
        // If adjusted score > underdog score, favorite covers
        // If adjusted score < underdog score, underdog covers
        // If adjusted score == underdog score, it's a push

        var adjustedFavoriteScore = favoriteScore + line;

        if (adjustedFavoriteScore > underdogScore)
        {
            // Favorite covered the spread
            return (favorite, false);
        }
        else if (adjustedFavoriteScore < underdogScore)
        {
            // Underdog covered the spread
            return (underdog, false);
        }
        else
        {
            // Push - scores are equal after spread adjustment
            return (null, true);
        }
    }
}
