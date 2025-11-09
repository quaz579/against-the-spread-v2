using AgainstTheSpread.Core.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace AgainstTheSpread.Functions;

/// <summary>
/// API endpoints for retrieving available weeks with lines
/// </summary>
public class WeeksFunction
{
    private readonly ILogger<WeeksFunction> _logger;
    private readonly IStorageService _storageService;

    public WeeksFunction(ILogger<WeeksFunction> logger, IStorageService storageService)
    {
        _logger = logger;
        _storageService = storageService;
    }

    /// <summary>
    /// GET /api/weeks?year={year}
    /// Returns list of week numbers that have lines available
    /// </summary>
    [Function("GetWeeks")]
    public async Task<HttpResponseData> GetWeeks(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "weeks")] HttpRequestData req,
        FunctionContext executionContext)
    {
        _logger.LogInformation("Processing GetWeeks request");

        try
        {
            // Get year from query string, default to current year
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var yearString = query["year"];
            var year = string.IsNullOrEmpty(yearString) 
                ? DateTime.UtcNow.Year 
                : int.Parse(yearString);

            var weeks = await _storageService.GetAvailableWeeksAsync(year);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                year,
                weeks
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available weeks");
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new { error = "Failed to retrieve available weeks" });
            return response;
        }
    }
}
