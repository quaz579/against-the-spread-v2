using AgainstTheSpread.Data.Interfaces;
using AgainstTheSpread.Functions.Helpers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace AgainstTheSpread.Functions;

/// <summary>
/// Azure Function for managing authenticated user bowl picks.
/// Provides endpoints for submitting and retrieving bowl picks with confidence points.
/// </summary>
public class UserBowlPicksFunction
{
    private readonly ILogger<UserBowlPicksFunction> _logger;
    private readonly IBowlPickService _bowlPickService;
    private readonly IBowlGameService _bowlGameService;
    private readonly IUserService _userService;
    private readonly AuthHelper _authHelper;

    public UserBowlPicksFunction(
        ILogger<UserBowlPicksFunction> logger,
        IBowlPickService bowlPickService,
        IBowlGameService bowlGameService,
        IUserService userService)
    {
        _logger = logger;
        _bowlPickService = bowlPickService;
        _bowlGameService = bowlGameService;
        _userService = userService;
        _authHelper = new AuthHelper(logger);
    }

    /// <summary>
    /// Submit or update bowl picks for a user.
    /// POST /api/user-bowl-picks
    /// </summary>
    [Function("SubmitUserBowlPicks")]
    public async Task<HttpResponseData> SubmitBowlPicks(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "user-bowl-picks")] HttpRequestData req)
    {
        _logger.LogInformation("Processing bowl pick submission request");

        try
        {
            // Validate authentication
            var authResult = await ValidateAndGetUserAsync(req);
            if (authResult.ErrorResponse != null)
            {
                return authResult.ErrorResponse;
            }

            // Get or create user in database
            var user = await _userService.GetOrCreateUserAsync(
                authResult.UserInfo!.UserId,
                authResult.UserInfo.Email,
                authResult.UserInfo.DisplayName ?? authResult.UserInfo.Email);

            // Parse request body
            var requestBody = await req.ReadAsStringAsync();
            if (string.IsNullOrEmpty(requestBody))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Request body is required");
            }

            var submitRequest = JsonSerializer.Deserialize<SubmitBowlPicksRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (submitRequest == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request format");
            }

            if (submitRequest.Year <= 0)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Year is required");
            }

            if (submitRequest.Picks == null || !submitRequest.Picks.Any())
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "At least one pick is required");
            }

            // Convert to service model
            var picks = submitRequest.Picks.Select(p => new BowlPickSubmission(
                p.BowlGameId,
                p.SpreadPick,
                p.ConfidencePoints,
                p.OutrightWinnerPick)).ToList();

            // Submit picks
            var result = await _bowlPickService.SubmitBowlPicksAsync(user.Id, submitRequest.Year, picks);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                success = result.Success,
                picksSubmitted = result.PicksSubmitted,
                picksRejected = result.PicksRejected,
                rejectedPicks = result.RejectedPicks.Select(r => new { bowlGameId = r.BowlGameId, reason = r.Reason }),
                message = result.Success
                    ? $"Successfully submitted {result.PicksSubmitted} bowl picks"
                    : result.ErrorMessage
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting bowl picks");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Failed to submit bowl picks");
        }
    }

    /// <summary>
    /// Get user's bowl picks for a specific year.
    /// GET /api/user-bowl-picks?year={year}
    /// </summary>
    [Function("GetUserBowlPicks")]
    public async Task<HttpResponseData> GetBowlPicks(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "user-bowl-picks")] HttpRequestData req)
    {
        _logger.LogInformation("Processing get bowl picks request");

        try
        {
            // Validate authentication
            var authResult = await ValidateAndGetUserAsync(req);
            if (authResult.ErrorResponse != null)
            {
                return authResult.ErrorResponse;
            }

            // Get year from query string
            if (!int.TryParse(req.Query["year"], out int year))
            {
                year = DateTime.UtcNow.Year;
            }

            // Get user from database
            var user = await _userService.GetByGoogleSubjectIdAsync(authResult.UserInfo!.UserId);
            if (user == null)
            {
                // Return empty picks if user hasn't submitted any yet
                var emptyResponse = req.CreateResponse(HttpStatusCode.OK);
                await emptyResponse.WriteAsJsonAsync(new
                {
                    year = year,
                    totalPicks = 0,
                    picks = Array.Empty<object>()
                });
                return emptyResponse;
            }

            // Get picks
            var picks = await _bowlPickService.GetUserBowlPicksAsync(user.Id, year);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                year = year,
                totalPicks = picks.Count,
                picks = picks.Select(p => new
                {
                    bowlGameId = p.BowlGameId,
                    spreadPick = p.SpreadPick,
                    confidencePoints = p.ConfidencePoints,
                    outrightWinnerPick = p.OutrightWinnerPick,
                    submittedAt = p.SubmittedAt,
                    updatedAt = p.UpdatedAt,
                    game = p.BowlGame != null ? new
                    {
                        id = p.BowlGame.Id,
                        gameNumber = p.BowlGame.GameNumber,
                        bowlName = p.BowlGame.BowlName,
                        favorite = p.BowlGame.Favorite,
                        underdog = p.BowlGame.Underdog,
                        line = p.BowlGame.Line,
                        gameDate = p.BowlGame.GameDate,
                        isLocked = p.BowlGame.IsLocked,
                        hasResult = p.BowlGame.HasResult,
                        spreadWinner = p.BowlGame.SpreadWinner,
                        outrightWinner = p.BowlGame.OutrightWinner,
                        isPush = p.BowlGame.IsPush
                    } : null
                })
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bowl picks");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Failed to get bowl picks");
        }
    }

    /// <summary>
    /// Get all bowl games for a year (available to all authenticated users).
    /// GET /api/bowl-games?year={year}
    /// </summary>
    [Function("GetBowlGames")]
    public async Task<HttpResponseData> GetBowlGames(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "bowl-games")] HttpRequestData req)
    {
        _logger.LogInformation("Processing get bowl games request");

        try
        {
            // Get year from query string
            if (!int.TryParse(req.Query["year"], out int year))
            {
                year = DateTime.UtcNow.Year;
            }

            // Get bowl games
            var games = await _bowlGameService.GetBowlGamesAsync(year);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                year = year,
                totalGames = games.Count,
                games = games.Select(g => new
                {
                    id = g.Id,
                    gameNumber = g.GameNumber,
                    bowlName = g.BowlName,
                    favorite = g.Favorite,
                    underdog = g.Underdog,
                    line = g.Line,
                    gameDate = g.GameDate,
                    isLocked = g.IsLocked,
                    hasResult = g.HasResult,
                    spreadWinner = g.SpreadWinner,
                    outrightWinner = g.OutrightWinner,
                    isPush = g.IsPush,
                    favoriteScore = g.FavoriteScore,
                    underdogScore = g.UnderdogScore
                })
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bowl games");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Failed to get bowl games");
        }
    }

    #region Helper Methods

    private async Task<(UserInfo? UserInfo, HttpResponseData? ErrorResponse)> ValidateAndGetUserAsync(HttpRequestData req)
    {
        var headers = req.Headers.ToDictionary(
            h => h.Key,
            h => h.Value,
            StringComparer.OrdinalIgnoreCase);

        var authResult = _authHelper.ValidateAuth(headers);
        if (!authResult.IsAuthenticated || authResult.User == null)
        {
            var statusCode = authResult.Error == "Authentication required"
                ? HttpStatusCode.Unauthorized
                : HttpStatusCode.Forbidden;

            var response = req.CreateResponse(statusCode);
            await response.WriteStringAsync(authResult.Error ?? "Authentication failed");
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

#region Request/Response Models

/// <summary>
/// Request model for submitting bowl picks.
/// </summary>
public class SubmitBowlPicksRequest
{
    public int Year { get; set; }
    public List<BowlPickInput>? Picks { get; set; }
}

/// <summary>
/// Individual bowl pick input in a submission request.
/// </summary>
public class BowlPickInput
{
    public int BowlGameId { get; set; }
    public string SpreadPick { get; set; } = string.Empty;
    public int ConfidencePoints { get; set; }
    public string OutrightWinnerPick { get; set; } = string.Empty;
}

#endregion
