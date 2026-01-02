using AgainstTheSpread.Core.Models;
using AgainstTheSpread.Data.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace AgainstTheSpread.Functions;

/// <summary>
/// API endpoint for retrieving weekly lines (games with betting info)
/// </summary>
public class LinesFunction
{
    private readonly ILogger<LinesFunction> _logger;
    private readonly IGameService _gameService;

    public LinesFunction(ILogger<LinesFunction> logger, IGameService gameService)
    {
        _logger = logger;
        _gameService = gameService;
    }

    /// <summary>
    /// GET /api/lines/{week}?year={year}
    /// Returns all games with lines for a specific week
    /// </summary>
    [Function("GetLines")]
    public async Task<HttpResponseData> GetLines(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "lines/{week}")] HttpRequestData req,
        int week)
    {
        _logger.LogInformation("Processing GetLines request for week {Week}", week);

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

            var gameEntities = await _gameService.GetWeekGamesAsync(year, week);

            if (gameEntities == null || gameEntities.Count == 0)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteAsJsonAsync(new
                {
                    error = $"Lines not found for week {week} of {year}"
                });
                return notFoundResponse;
            }

            // Transform database entities to WeeklyLines format for backward compatibility
            var lines = new WeeklyLines
            {
                Week = week,
                Year = year,
                UploadedAt = gameEntities.Min(g => g.GameDate), // Use earliest game date as proxy
                Games = gameEntities.Select(g => new Game
                {
                    Favorite = g.Favorite,
                    Underdog = g.Underdog,
                    Line = g.Line,
                    GameDate = g.GameDate,
                    VsAt = "vs" // Default to "vs" - neutral site indicator not stored in DB
                }).ToList()
            };

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(lines);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lines for week {Week}", week);
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "Failed to retrieve lines" });
            return errorResponse;
        }
    }
}
