using AgainstTheSpread.Data.Entities;
using AgainstTheSpread.Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgainstTheSpread.Data.Services;

/// <summary>
/// Service for calculating bowl game leaderboard standings.
/// </summary>
public class BowlLeaderboardService : IBowlLeaderboardService
{
    private readonly AtsDbContext _context;
    private readonly ILogger<BowlLeaderboardService> _logger;

    /// <summary>
    /// Initializes a new instance of BowlLeaderboardService.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    public BowlLeaderboardService(AtsDbContext context, ILogger<BowlLeaderboardService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<List<BowlLeaderboardEntry>> GetBowlLeaderboardAsync(
        int year,
        CancellationToken cancellationToken = default)
    {
        // Get all games and picks for the year
        var games = await _context.BowlGames
            .Where(g => g.Year == year)
            .ToListAsync(cancellationToken);

        var totalGames = games.Count;
        var gamesWithResults = games.Where(g => g.HasResult).ToList();
        var gameIds = games.Select(g => g.Id).ToList();

        // Get all picks for this year with user info
        var picks = await _context.BowlPicks
            .Include(p => p.User)
            .Include(p => p.BowlGame)
            .Where(p => p.Year == year)
            .ToListAsync(cancellationToken);

        // Group picks by user and calculate standings
        var userPicks = picks.GroupBy(p => p.UserId);
        var leaderboard = new List<BowlLeaderboardEntry>();

        foreach (var userGroup in userPicks)
        {
            var userId = userGroup.Key;
            var user = userGroup.First().User;
            var userPicksList = userGroup.ToList();

            var entry = new BowlLeaderboardEntry
            {
                UserId = userId,
                DisplayName = user?.DisplayName ?? "Unknown",
                TotalGames = totalGames,
                GamesCompleted = gamesWithResults.Count
            };

            // Calculate max possible points (sum of all confidence values assigned)
            entry.MaxPossiblePoints = userPicksList.Sum(p => p.ConfidencePoints);

            // Calculate results for each completed game
            foreach (var pick in userPicksList)
            {
                var game = pick.BowlGame;
                if (game == null || !game.HasResult)
                {
                    continue;
                }

                // Check spread pick result
                if (game.IsPush == true)
                {
                    entry.SpreadPushes++;
                }
                else if (game.SpreadWinner == pick.SpreadPick)
                {
                    entry.SpreadWins++;
                    entry.SpreadPoints += pick.ConfidencePoints;
                }
                else
                {
                    entry.SpreadLosses++;
                }

                // Check outright winner result
                if (game.OutrightWinner == pick.OutrightWinnerPick)
                {
                    entry.OutrightWins++;
                }
            }

            leaderboard.Add(entry);
        }

        // Sort by spread points descending, then by spread wins
        return leaderboard
            .OrderByDescending(e => e.SpreadPoints)
            .ThenByDescending(e => e.SpreadWins)
            .ThenByDescending(e => e.OutrightWins)
            .ToList();
    }

    /// <inheritdoc/>
    public async Task<BowlUserHistory?> GetUserBowlHistoryAsync(
        Guid userId,
        int year,
        CancellationToken cancellationToken = default)
    {
        // Get user
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            return null;
        }

        // Get all user picks for the year with game info
        var picks = await _context.BowlPicks
            .Include(p => p.BowlGame)
            .Where(p => p.UserId == userId && p.Year == year)
            .OrderBy(p => p.BowlGame!.GameNumber)
            .ToListAsync(cancellationToken);

        if (!picks.Any())
        {
            return null;
        }

        var history = new BowlUserHistory
        {
            UserId = userId,
            DisplayName = user.DisplayName,
            Year = year,
            MaxPossiblePoints = picks.Sum(p => p.ConfidencePoints)
        };

        foreach (var pick in picks)
        {
            var game = pick.BowlGame!;
            var detail = new BowlPickDetail
            {
                GameNumber = game.GameNumber,
                BowlName = game.BowlName,
                Favorite = game.Favorite,
                Underdog = game.Underdog,
                Line = game.Line,
                SpreadPick = pick.SpreadPick,
                ConfidencePoints = pick.ConfidencePoints,
                OutrightWinnerPick = pick.OutrightWinnerPick,
                HasResult = game.HasResult
            };

            if (game.HasResult)
            {
                detail.FavoriteScore = game.FavoriteScore;
                detail.UnderdogScore = game.UnderdogScore;
                detail.SpreadWinner = game.SpreadWinner;
                detail.IsPush = game.IsPush;
                detail.ActualOutrightWinner = game.OutrightWinner;

                // Calculate if picks were correct
                if (game.IsPush == true)
                {
                    detail.SpreadPickCorrect = null; // Push is neither correct nor incorrect
                    detail.PointsEarned = 0;
                }
                else
                {
                    detail.SpreadPickCorrect = game.SpreadWinner == pick.SpreadPick;
                    detail.PointsEarned = detail.SpreadPickCorrect == true ? pick.ConfidencePoints : 0;
                }

                detail.OutrightPickCorrect = game.OutrightWinner == pick.OutrightWinnerPick;
                history.TotalPoints += detail.PointsEarned;
            }

            history.Picks.Add(detail);
        }

        return history;
    }
}
