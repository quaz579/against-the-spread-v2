using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace AgainstTheSpread.Functions;

/// <summary>
/// API endpoint for retrieving application configuration flags
/// </summary>
public class GetConfigFunction
{
    private readonly ILogger<GetConfigFunction> _logger;

    public GetConfigFunction(ILogger<GetConfigFunction> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// GET /api/config
    /// Returns application configuration flags for the frontend
    /// </summary>
    [Function("GetConfig")]
    public async Task<HttpResponseData> GetConfig(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "config")] HttpRequestData req)
    {
        _logger.LogInformation("Processing GetConfig request");

        var gameLockingDisabled = Environment.GetEnvironmentVariable("DISABLE_GAME_LOCKING") == "true";

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            gameLockingDisabled
        });
        return response;
    }
}
