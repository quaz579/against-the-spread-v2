using AgainstTheSpread.Core.Models;
using AgainstTheSpread.Data.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace AgainstTheSpread.Functions;

/// <summary>
/// API endpoint for retrieving bowl game lines.
/// Separate from regular season lines endpoint.
/// </summary>
public class BowlLinesFunction
{
    private readonly ILogger<BowlLinesFunction> _logger;
    private readonly IBowlGameService _bowlGameService;

    public BowlLinesFunction(ILogger<BowlLinesFunction> logger, IBowlGameService bowlGameService)
    {
        _logger = logger;
        _bowlGameService = bowlGameService;
    }

    /// <summary>
    /// GET /api/bowl-lines?year={year}
    /// Returns all bowl games with lines for a specific year
    /// </summary>
    [Function("GetBowlLines")]
    public async Task<HttpResponseData> GetBowlLines(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "bowl-lines")] HttpRequestData req)
    {
        _logger.LogInformation("Processing GetBowlLines request");

        try
        {
            var yearString = req.Query["year"];
            var year = string.IsNullOrEmpty(yearString)
                ? DateTime.UtcNow.Year
                : int.Parse(yearString!);

            var gameEntities = await _bowlGameService.GetBowlGamesAsync(year);

            if (gameEntities == null || gameEntities.Count == 0)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteAsJsonAsync(new
                {
                    error = $"Bowl lines not found for {year}"
                });
                return notFoundResponse;
            }

            // Transform database entities to BowlLines format for backward compatibility
            var lines = new BowlLines
            {
                Year = year,
                UploadedAt = gameEntities.Min(g => g.GameDate), // Use earliest game date as proxy
                Games = gameEntities.Select(g => new BowlGame
                {
                    BowlName = g.BowlName,
                    GameNumber = g.GameNumber,
                    Favorite = g.Favorite,
                    Underdog = g.Underdog,
                    Line = g.Line,
                    GameDate = g.GameDate
                }).ToList()
            };

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(lines);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bowl lines");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "Failed to retrieve bowl lines" });
            return errorResponse;
        }
    }

    /// <summary>
    /// GET /api/bowl-lines/exists?year={year}
    /// Checks if bowl lines exist for a specific year
    /// </summary>
    [Function("BowlLinesExist")]
    public async Task<HttpResponseData> BowlLinesExist(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "bowl-lines/exists")] HttpRequestData req)
    {
        _logger.LogInformation("Processing BowlLinesExist request");

        try
        {
            var yearString = req.Query["year"];
            var year = string.IsNullOrEmpty(yearString)
                ? DateTime.UtcNow.Year
                : int.Parse(yearString!);

            var exists = await _bowlGameService.BowlGamesExistAsync(year);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new { year, exists });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking bowl lines existence");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "Failed to check bowl lines" });
            return errorResponse;
        }
    }
}
