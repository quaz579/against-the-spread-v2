using AgainstTheSpread.Data.Interfaces;
using AgainstTheSpread.Functions.Helpers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace AgainstTheSpread.Functions;

/// <summary>
/// Azure Function for managing bowl game results.
/// Admin only endpoints for entering results.
/// </summary>
public class BowlResultsFunction
{
    private readonly ILogger<BowlResultsFunction> _logger;
    private readonly IBowlGameService _bowlGameService;
    private readonly IUserService _userService;
    private readonly AuthHelper _authHelper;

    public BowlResultsFunction(
        ILogger<BowlResultsFunction> logger,
        IBowlGameService bowlGameService,
        IUserService userService)
    {
        _logger = logger;
        _bowlGameService = bowlGameService;
        _userService = userService;
        _authHelper = new AuthHelper(logger);
    }

    /// <summary>
    /// Submit results for multiple bowl games.
    /// POST /api/bowl-results
    /// Admin only.
    /// </summary>
    [Function("SubmitBowlResults")]
    public async Task<HttpResponseData> SubmitBowlResults(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "bowl-results")] HttpRequestData req)
    {
        _logger.LogInformation("Processing submit bowl results request");

        try
        {
            // Validate admin access
            var authResult = await ValidateAdminAccessAsync(req);
            if (authResult.ErrorResponse != null)
            {
                return authResult.ErrorResponse;
            }

            // Get or create admin user
            var adminUser = await _userService.GetOrCreateUserAsync(
                authResult.UserInfo!.UserId,
                authResult.UserInfo.Email,
                authResult.UserInfo.DisplayName ?? authResult.UserInfo.Email);

            // Parse request body
            var requestBody = await req.ReadAsStringAsync();
            if (string.IsNullOrEmpty(requestBody))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Request body is required");
            }

            var submitRequest = JsonSerializer.Deserialize<SubmitBowlResultsRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (submitRequest?.Results == null || !submitRequest.Results.Any())
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "At least one result is required");
            }

            // Convert to service model
            var results = submitRequest.Results.Select(r =>
                new BowlGameResultInput(r.BowlGameId, r.FavoriteScore, r.UnderdogScore));

            // Enter results
            var result = await _bowlGameService.BulkEnterResultsAsync(results, adminUser.Id);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                success = result.Success,
                resultsEntered = result.ResultsEntered,
                resultsFailed = result.ResultsFailed,
                failedResults = result.FailedResults.Select(f => new { bowlGameId = f.BowlGameId, reason = f.Reason }),
                message = result.Success
                    ? $"Successfully entered {result.ResultsEntered} bowl results"
                    : result.ErrorMessage
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting bowl results");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Failed to submit bowl results");
        }
    }

    /// <summary>
    /// Enter result for a single bowl game.
    /// POST /api/bowl-results/game/{gameId}
    /// Admin only.
    /// </summary>
    [Function("SubmitSingleBowlResult")]
    public async Task<HttpResponseData> SubmitSingleBowlResult(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "bowl-results/game/{gameId:int}")] HttpRequestData req,
        int gameId)
    {
        _logger.LogInformation("Processing submit single bowl result request for game {GameId}", gameId);

        try
        {
            // Validate admin access
            var authResult = await ValidateAdminAccessAsync(req);
            if (authResult.ErrorResponse != null)
            {
                return authResult.ErrorResponse;
            }

            // Get or create admin user
            var adminUser = await _userService.GetOrCreateUserAsync(
                authResult.UserInfo!.UserId,
                authResult.UserInfo.Email,
                authResult.UserInfo.DisplayName ?? authResult.UserInfo.Email);

            // Parse request body
            var requestBody = await req.ReadAsStringAsync();
            if (string.IsNullOrEmpty(requestBody))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Request body is required");
            }

            var submitRequest = JsonSerializer.Deserialize<SingleBowlResultRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (submitRequest == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request format");
            }

            // Enter result
            var game = await _bowlGameService.EnterResultAsync(
                gameId,
                submitRequest.FavoriteScore,
                submitRequest.UnderdogScore,
                adminUser.Id);

            if (game == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.NotFound, "Bowl game not found");
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                success = true,
                game = new
                {
                    id = game.Id,
                    gameNumber = game.GameNumber,
                    bowlName = game.BowlName,
                    favorite = game.Favorite,
                    underdog = game.Underdog,
                    line = game.Line,
                    favoriteScore = game.FavoriteScore,
                    underdogScore = game.UnderdogScore,
                    spreadWinner = game.SpreadWinner,
                    outrightWinner = game.OutrightWinner,
                    isPush = game.IsPush,
                    resultEnteredAt = game.ResultEnteredAt
                },
                message = $"Result entered: {game.Favorite} {game.FavoriteScore} - {game.Underdog} {game.UnderdogScore}"
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting bowl result for game {GameId}", gameId);
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Failed to submit bowl result");
        }
    }

    #region Helper Methods

    private async Task<(UserInfo? UserInfo, HttpResponseData? ErrorResponse)> ValidateAdminAccessAsync(HttpRequestData req)
    {
        var headers = req.Headers.ToDictionary(
            h => h.Key,
            h => h.Value,
            StringComparer.OrdinalIgnoreCase);

        var authResult = _authHelper.ValidateAuth(headers);

        // Not authenticated
        if (!authResult.IsAuthenticated || authResult.User == null)
        {
            var response = req.CreateResponse(HttpStatusCode.Unauthorized);
            await response.WriteStringAsync(authResult.Error ?? "Authentication required");
            return (null, response);
        }

        // Check if user is admin
        if (!_authHelper.IsAdmin(authResult.User))
        {
            var response = req.CreateResponse(HttpStatusCode.Forbidden);
            await response.WriteStringAsync("Admin access required");
            return (null, response);
        }

        return (authResult.User, null);
    }

    private async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, HttpStatusCode statusCode, string message)
    {
        var response = req.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(new { error = message });
        return response;
    }

    #endregion
}

#region Request Models

public class SubmitBowlResultsRequest
{
    public List<BowlResultInput>? Results { get; set; }
}

public class BowlResultInput
{
    public int BowlGameId { get; set; }
    public int FavoriteScore { get; set; }
    public int UnderdogScore { get; set; }
}

public class SingleBowlResultRequest
{
    public int FavoriteScore { get; set; }
    public int UnderdogScore { get; set; }
}

#endregion
