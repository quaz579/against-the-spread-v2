using AgainstTheSpread.Core.Interfaces;

namespace AgainstTheSpread.Data.Interfaces;

/// <summary>
/// Service for matching external API game results to internal database games.
/// Handles team name normalization and score mapping from Home/Away to Favorite/Underdog.
/// </summary>
public interface IGameResultMatcher
{
    /// <summary>
    /// Matches external game results to games in the database.
    /// Uses team name normalization to match games and maps scores correctly.
    /// </summary>
    /// <param name="year">The season year.</param>
    /// <param name="week">The week number.</param>
    /// <param name="externalResults">External game results from the API.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Response containing matched results, unmatched games, and skip counts.</returns>
    Task<MatchResultsResponse> MatchResultsToGamesAsync(
        int year, int week, List<ExternalGameResult> externalResults, CancellationToken cancellationToken = default);
}

/// <summary>
/// A game result that has been matched to a database game.
/// </summary>
public class MatchedGameResult
{
    /// <summary>
    /// The database game ID.
    /// </summary>
    public int GameId { get; set; }

    /// <summary>
    /// The favorite team's score.
    /// </summary>
    public int FavoriteScore { get; set; }

    /// <summary>
    /// The underdog team's score.
    /// </summary>
    public int UnderdogScore { get; set; }
}

/// <summary>
/// An external game result that could not be matched to a database game.
/// </summary>
public class UnmatchedResult
{
    /// <summary>
    /// The home team name from the external API.
    /// </summary>
    public string HomeTeam { get; set; } = string.Empty;

    /// <summary>
    /// The away team name from the external API.
    /// </summary>
    public string AwayTeam { get; set; } = string.Empty;

    /// <summary>
    /// The reason the game could not be matched.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Response from the game result matching operation.
/// </summary>
public class MatchResultsResponse
{
    /// <summary>
    /// Games that were successfully matched to database games.
    /// </summary>
    public List<MatchedGameResult> Matched { get; set; } = new();

    /// <summary>
    /// Games that could not be matched to database games.
    /// </summary>
    public List<UnmatchedResult> Unmatched { get; set; } = new();

    /// <summary>
    /// Count of games that were skipped because they already have results.
    /// </summary>
    public int GamesWithExistingResults { get; set; }
}
