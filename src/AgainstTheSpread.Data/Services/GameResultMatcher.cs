using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Data.Interfaces;
using Microsoft.Extensions.Logging;

namespace AgainstTheSpread.Data.Services;

/// <summary>
/// Service for matching external API game results to internal database games.
/// Uses team name normalization to match games from external sources to database games.
/// </summary>
public class GameResultMatcher : IGameResultMatcher
{
    private readonly IGameService _gameService;
    private readonly ITeamNameNormalizer _teamNameNormalizer;
    private readonly ILogger<GameResultMatcher> _logger;

    public GameResultMatcher(
        IGameService gameService,
        ITeamNameNormalizer teamNameNormalizer,
        ILogger<GameResultMatcher> logger)
    {
        _gameService = gameService;
        _teamNameNormalizer = teamNameNormalizer;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<MatchResultsResponse> MatchResultsToGamesAsync(
        int year, int week, List<ExternalGameResult> externalResults, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Matching {Count} external results to games for year {Year} week {Week}",
            externalResults.Count, year, week);

        var response = new MatchResultsResponse();

        if (!externalResults.Any())
        {
            _logger.LogInformation("No external results to match");
            return response;
        }

        // Get all games for this week from the database
        var games = await _gameService.GetWeekGamesAsync(year, week, cancellationToken);

        if (!games.Any())
        {
            _logger.LogWarning("No games found in database for year {Year} week {Week}", year, week);
            foreach (var result in externalResults)
            {
                response.Unmatched.Add(new UnmatchedResult
                {
                    HomeTeam = result.HomeTeam,
                    AwayTeam = result.AwayTeam,
                    Reason = "No games found in database for this week"
                });
            }
            return response;
        }

        // Collect all team names for batch normalization
        var allTeamNames = new List<string>();
        allTeamNames.AddRange(externalResults.SelectMany(r => new[] { r.HomeTeam, r.AwayTeam }));
        allTeamNames.AddRange(games.SelectMany(g => new[] { g.Favorite, g.Underdog }));

        // Normalize all team names in batch for efficiency
        var normalizedNames = await _teamNameNormalizer.NormalizeBatchAsync(allTeamNames, cancellationToken);

        // Build a lookup dictionary for games by normalized team names
        var gamesByTeams = new Dictionary<string, (int GameId, string Favorite, string Underdog, bool HasResult)>();

        foreach (var game in games)
        {
            var normalizedFavorite = normalizedNames.GetValueOrDefault(game.Favorite, game.Favorite);
            var normalizedUnderdog = normalizedNames.GetValueOrDefault(game.Underdog, game.Underdog);

            // Create lookup key using sorted team names to handle both home/away orderings
            var key1 = CreateTeamKey(normalizedFavorite, normalizedUnderdog);

            if (!gamesByTeams.ContainsKey(key1))
            {
                gamesByTeams[key1] = (game.Id, normalizedFavorite, normalizedUnderdog, game.HasResult);
            }
            else
            {
                _logger.LogWarning("Duplicate game found for teams {Favorite} vs {Underdog}", game.Favorite, game.Underdog);
            }
        }

        // Match each external result to a database game
        foreach (var externalResult in externalResults)
        {
            // Skip incomplete games
            if (!externalResult.IsCompleted)
            {
                _logger.LogDebug("Skipping incomplete game: {HomeTeam} vs {AwayTeam}",
                    externalResult.HomeTeam, externalResult.AwayTeam);
                continue;
            }

            var normalizedHomeTeam = normalizedNames.GetValueOrDefault(externalResult.HomeTeam, externalResult.HomeTeam);
            var normalizedAwayTeam = normalizedNames.GetValueOrDefault(externalResult.AwayTeam, externalResult.AwayTeam);

            var lookupKey = CreateTeamKey(normalizedHomeTeam, normalizedAwayTeam);

            if (!gamesByTeams.TryGetValue(lookupKey, out var gameInfo))
            {
                _logger.LogWarning("Could not find database game for {HomeTeam} vs {AwayTeam}",
                    externalResult.HomeTeam, externalResult.AwayTeam);

                response.Unmatched.Add(new UnmatchedResult
                {
                    HomeTeam = externalResult.HomeTeam,
                    AwayTeam = externalResult.AwayTeam,
                    Reason = "Game not found in database"
                });
                continue;
            }

            // Skip if game already has results
            if (gameInfo.HasResult)
            {
                _logger.LogDebug("Skipping game {GameId} - already has results", gameInfo.GameId);
                response.GamesWithExistingResults++;
                continue;
            }

            // Determine which team is the favorite to correctly map scores
            // The database stores Favorite/Underdog, the API returns Home/Away
            int favoriteScore, underdogScore;

            // Check if home team is the favorite (after normalization)
            if (string.Equals(normalizedHomeTeam, gameInfo.Favorite, StringComparison.OrdinalIgnoreCase))
            {
                // Home team is favorite
                favoriteScore = externalResult.HomeScore;
                underdogScore = externalResult.AwayScore;
            }
            else
            {
                // Away team is favorite
                favoriteScore = externalResult.AwayScore;
                underdogScore = externalResult.HomeScore;
            }

            response.Matched.Add(new MatchedGameResult
            {
                GameId = gameInfo.GameId,
                FavoriteScore = favoriteScore,
                UnderdogScore = underdogScore
            });

            _logger.LogDebug("Matched game {GameId}: {HomeTeam} {HomeScore} vs {AwayTeam} {AwayScore} -> Fav: {FavScore}, Dog: {DogScore}",
                gameInfo.GameId, externalResult.HomeTeam, externalResult.HomeScore,
                externalResult.AwayTeam, externalResult.AwayScore, favoriteScore, underdogScore);
        }

        _logger.LogInformation(
            "Matching complete: {Matched} matched, {Unmatched} unmatched, {Skipped} already had results",
            response.Matched.Count, response.Unmatched.Count, response.GamesWithExistingResults);

        return response;
    }

    /// <summary>
    /// Creates a consistent lookup key from two team names, sorted alphabetically.
    /// </summary>
    private static string CreateTeamKey(string team1, string team2)
    {
        var names = new[] { team1.ToLowerInvariant(), team2.ToLowerInvariant() };
        Array.Sort(names);
        return $"{names[0]}|{names[1]}";
    }
}
