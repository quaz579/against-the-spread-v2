using AgainstTheSpread.Core.Interfaces;
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
    private readonly IStorageService _storageService;

    public BowlLinesFunction(ILogger<BowlLinesFunction> logger, IStorageService storageService)
    {
        _logger = logger;
        _storageService = storageService;
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

            var lines = await _storageService.GetBowlLinesAsync(year);

            if (lines == null)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteAsJsonAsync(new
                {
                    error = $"Bowl lines not found for {year}"
                });
                return notFoundResponse;
            }

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

            var exists = await _storageService.BowlLinesExistAsync(year);

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
