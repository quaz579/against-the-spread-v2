using AgainstTheSpread.Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgainstTheSpread.Data.Services;

/// <summary>
/// Service for calculating and retrieving leaderboard standings.
/// </summary>
public class LeaderboardService : ILeaderboardService
{
    private readonly AtsDbContext _context;
    private readonly ILogger<LeaderboardService> _logger;

    public LeaderboardService(AtsDbContext context, ILogger<LeaderboardService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<List<WeeklyLeaderboardEntry>> GetWeeklyLeaderboardAsync(
        int year,
        int week,
        CancellationToken cancellationToken = default)
    {
        // Get all picks for the week with their results
        // Note: HasResult is a computed property, so we use the underlying fields for DB queries
        var picks = await _context.Picks
            .Include(p => p.User)
            .Include(p => p.Game)
            .Where(p => p.Year == year && p.Week == week && (p.Game!.SpreadWinner != null || p.Game.IsPush == true))
            .ToListAsync(cancellationToken);

        // Group by user and calculate stats
        var userStats = picks
            .GroupBy(p => new { p.UserId, p.User!.DisplayName })
            .Select(g =>
            {
                var wins = 0m;
                var losses = 0m;
                var pushes = 0;

                foreach (var pick in g)
                {
                    if (pick.Game!.IsPush == true)
                    {
                        pushes++;
                        wins += 0.5m; // Pushes count as half win
                    }
                    else if (pick.Game.SpreadWinner == pick.SelectedTeam)
                    {
                        wins += 1m;
                    }
                    else
                    {
                        losses += 1m;
                    }
                }

                var totalGames = wins + losses - (pushes * 0.5m) + pushes;
                var winPercentage = totalGames > 0 ? Math.Round((wins / totalGames) * 100, 1) : 0;

                return new WeeklyLeaderboardEntry(
                    g.Key.UserId,
                    g.Key.DisplayName,
                    week,
                    wins,
                    losses,
                    pushes,
                    winPercentage);
            })
            .OrderByDescending(e => e.Wins)
            .ThenBy(e => e.Losses)
            .ThenBy(e => e.DisplayName)
            .ToList();

        _logger.LogInformation("Generated weekly leaderboard for {Year} week {Week}: {Count} entries", year, week, userStats.Count);

        return userStats;
    }

    /// <inheritdoc/>
    public async Task<List<SeasonLeaderboardEntry>> GetSeasonLeaderboardAsync(
        int year,
        CancellationToken cancellationToken = default)
    {
        // Get all picks for the year with their results
        // Note: HasResult is a computed property, so we use the underlying fields for DB queries
        var picks = await _context.Picks
            .Include(p => p.User)
            .Include(p => p.Game)
            .Where(p => p.Year == year && (p.Game!.SpreadWinner != null || p.Game.IsPush == true))
            .ToListAsync(cancellationToken);

        // Group by user and calculate season stats
        var userStats = picks
            .GroupBy(p => new { p.UserId, p.User!.DisplayName })
            .Select(g =>
            {
                var totalWins = 0m;
                var totalLosses = 0m;
                var totalPushes = 0;
                var weeksPlayed = new HashSet<int>();
                var weekWins = new Dictionary<int, decimal>();

                foreach (var pick in g)
                {
                    weeksPlayed.Add(pick.Week);

                    if (!weekWins.ContainsKey(pick.Week))
                        weekWins[pick.Week] = 0;

                    if (pick.Game!.IsPush == true)
                    {
                        totalPushes++;
                        totalWins += 0.5m;
                        weekWins[pick.Week] += 0.5m;
                    }
                    else if (pick.Game.SpreadWinner == pick.SelectedTeam)
                    {
                        totalWins += 1m;
                        weekWins[pick.Week] += 1m;
                    }
                    else
                    {
                        totalLosses += 1m;
                    }
                }

                // A perfect week is 6/6 (6 wins)
                var perfectWeeks = weekWins.Count(kv => kv.Value >= 6);

                var totalGames = totalWins + totalLosses - (totalPushes * 0.5m) + totalPushes;
                var winPercentage = totalGames > 0 ? Math.Round((totalWins / totalGames) * 100, 1) : 0;

                return new SeasonLeaderboardEntry(
                    g.Key.UserId,
                    g.Key.DisplayName,
                    totalWins,
                    totalLosses,
                    totalPushes,
                    winPercentage,
                    weeksPlayed.Count,
                    perfectWeeks);
            })
            .OrderByDescending(e => e.TotalWins)
            .ThenByDescending(e => e.WinPercentage)
            .ThenBy(e => e.DisplayName)
            .ToList();

        _logger.LogInformation("Generated season leaderboard for {Year}: {Count} entries", year, userStats.Count);

        return userStats;
    }

    /// <inheritdoc/>
    public async Task<UserSeasonHistory?> GetUserSeasonHistoryAsync(
        Guid userId,
        int year,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found for season history", userId);
            return null;
        }

        var picks = await _context.Picks
            .Include(p => p.Game)
            .Where(p => p.UserId == userId && p.Year == year)
            .OrderBy(p => p.Week)
            .ThenBy(p => p.Game!.GameDate)
            .ToListAsync(cancellationToken);

        if (!picks.Any())
        {
            return new UserSeasonHistory
            {
                UserId = userId,
                DisplayName = user.DisplayName,
                Year = year
            };
        }

        var history = new UserSeasonHistory
        {
            UserId = userId,
            DisplayName = user.DisplayName,
            Year = year
        };

        // Group by week
        var weekGroups = picks.GroupBy(p => p.Week).OrderBy(g => g.Key);

        foreach (var weekGroup in weekGroups)
        {
            var weekHistory = new UserWeekHistory
            {
                Week = weekGroup.Key
            };

            foreach (var pick in weekGroup)
            {
                var pickResult = new UserPickResult
                {
                    GameId = pick.GameId,
                    Favorite = pick.Game!.Favorite,
                    Underdog = pick.Game.Underdog,
                    Line = pick.Game.Line,
                    SelectedTeam = pick.SelectedTeam,
                    SpreadWinner = pick.Game.SpreadWinner,
                    IsPush = pick.Game.IsPush,
                    HasResult = pick.Game.HasResult
                };

                if (pick.Game.HasResult)
                {
                    if (pick.Game.IsPush == true)
                    {
                        weekHistory.Pushes++;
                        weekHistory.Wins += 0.5m;
                        history.TotalPushes++;
                        history.TotalWins += 0.5m;
                        pickResult.IsWin = null; // Push
                    }
                    else if (pick.Game.SpreadWinner == pick.SelectedTeam)
                    {
                        weekHistory.Wins += 1m;
                        history.TotalWins += 1m;
                        pickResult.IsWin = true;
                    }
                    else
                    {
                        weekHistory.Losses += 1m;
                        history.TotalLosses += 1m;
                        pickResult.IsWin = false;
                    }
                }

                weekHistory.Picks.Add(pickResult);
            }

            weekHistory.IsPerfect = weekHistory.Wins >= 6 && weekHistory.Losses == 0;
            history.Weeks.Add(weekHistory);
        }

        var totalGames = history.TotalWins + history.TotalLosses - (history.TotalPushes * 0.5m) + history.TotalPushes;
        history.WinPercentage = totalGames > 0 ? Math.Round((history.TotalWins / totalGames) * 100, 1) : 0;

        _logger.LogInformation("Generated season history for user {UserId} year {Year}: {Weeks} weeks, {Wins} wins",
            userId, year, history.Weeks.Count, history.TotalWins);

        return history;
    }
}
