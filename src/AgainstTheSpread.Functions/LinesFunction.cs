using AgainstTheSpread.Core.Interfaces;
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
    private readonly IStorageService _storageService;

    public LinesFunction(ILogger<LinesFunction> logger, IStorageService storageService)
    {
        _logger = logger;
        _storageService = storageService;
    }

    /// <summary>
    /// GET /api/lines/{week}?year={year}
    /// Returns all games with lines for a specific week
    /// </summary>
    [Function("GetLines")]
    public async Task<HttpResponseData> GetLines(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "lines/{week}")] HttpRequestData req,
        int week,
        FunctionContext executionContext)
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
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var yearString = query["year"];
            var year = string.IsNullOrEmpty(yearString)
                ? DateTime.UtcNow.Year
                : int.Parse(yearString);

            var lines = await _storageService.GetLinesAsync(week, year);

            if (lines == null)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteAsJsonAsync(new 
                { 
                    error = $"Lines not found for week {week} of {year}" 
                });
                return notFoundResponse;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(lines);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lines for week {Week}", week);
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new { error = "Failed to retrieve lines" });
            return response;
        }
    }
}
