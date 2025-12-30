using AgainstTheSpread.Data.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace AgainstTheSpread.Functions;

/// <summary>
/// API endpoint for retrieving games from the database.
/// Returns games with database IDs for authenticated pick submission.
/// </summary>
public class GamesFunction
{
    private readonly ILogger<GamesFunction> _logger;
    private readonly IGameService _gameService;

    public GamesFunction(ILogger<GamesFunction> logger, IGameService gameService)
    {
        _logger = logger;
        _gameService = gameService;
    }

    /// <summary>
    /// GET /api/games/{week}?year={year}
    /// Returns all games from the database for a specific week, including lock status.
    /// </summary>
    [Function("GetGames")]
    public async Task<HttpResponseData> GetGames(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "games/{week}")] HttpRequestData req,
        int week)
    {
        _logger.LogInformation("Processing GetGames request for week {Week}", week);

        try
        {
            // Validate week number
            if (week < 1 || week > 14)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Week must be between 1 and 14" });
                return badResponse;
            }

            // Get year from query string, default to current year
            var yearString = req.Query["year"];
            var year = string.IsNullOrEmpty(yearString)
                ? DateTime.UtcNow.Year
                : int.Parse(yearString!);

            var games = await _gameService.GetWeekGamesAsync(year, week);

            if (games == null || !games.Any())
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteAsJsonAsync(new
                {
                    error = $"No games found for week {week} of {year}. Games may not have been synced to the database yet."
                });
                return notFoundResponse;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                year = year,
                week = week,
                totalGames = games.Count,
                games = games.Select(g => new
                {
                    id = g.Id,
                    favorite = g.Favorite,
                    underdog = g.Underdog,
                    line = g.Line,
                    gameDate = g.GameDate,
                    isLocked = g.IsLocked,
                    hasResult = g.HasResult,
                    spreadWinner = g.SpreadWinner,
                    isPush = g.IsPush
                })
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting games for week {Week}", week);
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "Failed to retrieve games" });
            return errorResponse;
        }
    }
}
