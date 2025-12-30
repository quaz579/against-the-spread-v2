using AgainstTheSpread.Data.Interfaces;
using AgainstTheSpread.Functions.Helpers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace AgainstTheSpread.Functions;

/// <summary>
/// Azure Function for managing game results.
/// Provides endpoints for entering and retrieving game results.
/// </summary>
public class ResultsFunction
{
    private readonly ILogger<ResultsFunction> _logger;
    private readonly IResultService _resultService;
    private readonly IUserService _userService;
    private readonly AuthHelper _authHelper;

    public ResultsFunction(
        ILogger<ResultsFunction> logger,
        IResultService resultService,
        IUserService userService)
    {
        _logger = logger;
        _resultService = resultService;
        _userService = userService;
        _authHelper = new AuthHelper(logger);
    }

    /// <summary>
    /// Submit results for a week.
    /// POST /api/results/{week}?year={year}
    /// Admin only.
    /// </summary>
    [Function("SubmitResults")]
    public async Task<HttpResponseData> SubmitResults(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "results/{week:int}")] HttpRequestData req,
        int week)
    {
        _logger.LogInformation("Processing submit results request for week {Week}", week);

        try
        {
            // Validate admin access
            var authResult = ValidateAdminAccess(req);
            if (authResult.ErrorResponse != null)
            {
                return authResult.ErrorResponse;
            }

            // Get year from query string
            if (!int.TryParse(req.Query["year"], out int year))
            {
                year = DateTime.Now.Year;
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

            var submitRequest = JsonSerializer.Deserialize<SubmitResultsRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (submitRequest?.Results == null || !submitRequest.Results.Any())
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "At least one result is required");
            }

            // Convert to service model
            var results = submitRequest.Results.Select(r =>
                new GameResultInput(r.GameId, r.FavoriteScore, r.UnderdogScore));

            // Enter results
            var result = await _resultService.BulkEnterResultsAsync(year, week, results, adminUser.Id);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                success = result.Success,
                resultsEntered = result.ResultsEntered,
                resultsFailed = result.ResultsFailed,
                failedResults = result.FailedResults.Select(f => new { gameId = f.GameId, reason = f.Reason }),
                message = result.Success
                    ? $"Successfully entered {result.ResultsEntered} results"
                    : result.ErrorMessage
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting results for week {Week}", week);
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Failed to submit results");
        }
    }

    /// <summary>
    /// Get results for a week.
    /// GET /api/results/{week}?year={year}
    /// Public access.
    /// </summary>
    [Function("GetResults")]
    public async Task<HttpResponseData> GetResults(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "results/{week:int}")] HttpRequestData req,
        int week)
    {
        _logger.LogInformation("Processing get results request for week {Week}", week);

        try
        {
            // Get year from query string
            if (!int.TryParse(req.Query["year"], out int year))
            {
                year = DateTime.Now.Year;
            }

            var games = await _resultService.GetWeekResultsAsync(year, week);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                year = year,
                week = week,
                totalGames = games.Count,
                gamesWithResults = games.Count(g => g.HasResult),
                games = games.Select(g => new
                {
                    id = g.Id,
                    favorite = g.Favorite,
                    underdog = g.Underdog,
                    line = g.Line,
                    gameDate = g.GameDate,
                    isLocked = g.IsLocked,
                    hasResult = g.HasResult,
                    favoriteScore = g.FavoriteScore,
                    underdogScore = g.UnderdogScore,
                    spreadWinner = g.SpreadWinner,
                    isPush = g.IsPush,
                    resultEnteredAt = g.ResultEnteredAt
                })
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting results for week {Week}", week);
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Failed to get results");
        }
    }

    /// <summary>
    /// Enter result for a single game.
    /// POST /api/results/game/{gameId}
    /// Admin only.
    /// </summary>
    [Function("SubmitSingleResult")]
    public async Task<HttpResponseData> SubmitSingleResult(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "results/game/{gameId:int}")] HttpRequestData req,
        int gameId)
    {
        _logger.LogInformation("Processing submit single result request for game {GameId}", gameId);

        try
        {
            // Validate admin access
            var authResult = ValidateAdminAccess(req);
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

            var submitRequest = JsonSerializer.Deserialize<SingleResultRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (submitRequest == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request format");
            }

            // Enter result
            var game = await _resultService.EnterResultAsync(
                gameId,
                submitRequest.FavoriteScore,
                submitRequest.UnderdogScore,
                adminUser.Id);

            if (game == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.NotFound, "Game not found");
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                success = true,
                game = new
                {
                    id = game.Id,
                    favorite = game.Favorite,
                    underdog = game.Underdog,
                    line = game.Line,
                    favoriteScore = game.FavoriteScore,
                    underdogScore = game.UnderdogScore,
                    spreadWinner = game.SpreadWinner,
                    isPush = game.IsPush,
                    resultEnteredAt = game.ResultEnteredAt
                },
                message = $"Result entered: {game.Favorite} {game.FavoriteScore} - {game.Underdog} {game.UnderdogScore}"
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting result for game {GameId}", gameId);
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Failed to submit result");
        }
    }

    #region Helper Methods

    private (UserInfo? UserInfo, HttpResponseData? ErrorResponse) ValidateAdminAccess(HttpRequestData req)
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
            response.WriteStringAsync(authResult.Error ?? "Authentication required").Wait();
            return (null, response);
        }

        // Check if user is admin
        if (!_authHelper.IsAdmin(authResult.User))
        {
            var response = req.CreateResponse(HttpStatusCode.Forbidden);
            response.WriteStringAsync("Admin access required").Wait();
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

public class SubmitResultsRequest
{
    public List<ResultInput>? Results { get; set; }
}

public class ResultInput
{
    public int GameId { get; set; }
    public int FavoriteScore { get; set; }
    public int UnderdogScore { get; set; }
}

public class SingleResultRequest
{
    public int FavoriteScore { get; set; }
    public int UnderdogScore { get; set; }
}

#endregion
