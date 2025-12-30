using AgainstTheSpread.Data.Interfaces;
using AgainstTheSpread.Functions.Helpers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace AgainstTheSpread.Functions;

/// <summary>
/// Azure Function for bowl game leaderboard and user history.
/// </summary>
public class BowlLeaderboardFunction
{
    private readonly ILogger<BowlLeaderboardFunction> _logger;
    private readonly IBowlLeaderboardService _bowlLeaderboardService;
    private readonly IUserService _userService;
    private readonly AuthHelper _authHelper;

    public BowlLeaderboardFunction(
        ILogger<BowlLeaderboardFunction> logger,
        IBowlLeaderboardService bowlLeaderboardService,
        IUserService userService)
    {
        _logger = logger;
        _bowlLeaderboardService = bowlLeaderboardService;
        _userService = userService;
        _authHelper = new AuthHelper(logger);
    }

    /// <summary>
    /// Get bowl leaderboard for a year.
    /// GET /api/bowl-leaderboard?year={year}
    /// </summary>
    [Function("GetBowlLeaderboard")]
    public async Task<HttpResponseData> GetBowlLeaderboard(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "bowl-leaderboard")] HttpRequestData req)
    {
        _logger.LogInformation("Processing get bowl leaderboard request");

        try
        {
            // Get year from query string
            if (!int.TryParse(req.Query["year"], out int year))
            {
                year = DateTime.UtcNow.Year;
            }

            // Get leaderboard
            var leaderboard = await _bowlLeaderboardService.GetBowlLeaderboardAsync(year);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                year = year,
                totalEntries = leaderboard.Count,
                entries = leaderboard.Select((e, index) => new
                {
                    rank = index + 1,
                    userId = e.UserId,
                    displayName = e.DisplayName,
                    spreadPoints = e.SpreadPoints,
                    spreadWins = e.SpreadWins,
                    spreadLosses = e.SpreadLosses,
                    spreadPushes = e.SpreadPushes,
                    outrightWins = e.OutrightWins,
                    maxPossiblePoints = e.MaxPossiblePoints,
                    pointsPercentage = e.PointsPercentage,
                    gamesCompleted = e.GamesCompleted,
                    totalGames = e.TotalGames
                })
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bowl leaderboard");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "Failed to get bowl leaderboard" });
            return errorResponse;
        }
    }

    /// <summary>
    /// Get user's bowl history for a year.
    /// GET /api/bowl-history/{userId}?year={year}
    /// </summary>
    [Function("GetBowlUserHistory")]
    public async Task<HttpResponseData> GetUserBowlHistory(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "bowl-history/{userId}")] HttpRequestData req,
        string userId)
    {
        _logger.LogInformation("Processing get bowl history request for user {UserId}", userId);

        try
        {
            // Get year from query string
            if (!int.TryParse(req.Query["year"], out int year))
            {
                year = DateTime.UtcNow.Year;
            }

            // Parse user ID
            if (!Guid.TryParse(userId, out Guid userGuid))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Invalid user ID format" });
                return badResponse;
            }

            // Get user history
            var history = await _bowlLeaderboardService.GetUserBowlHistoryAsync(userGuid, year);

            if (history == null)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteAsJsonAsync(new { error = "User history not found" });
                return notFoundResponse;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                userId = history.UserId,
                displayName = history.DisplayName,
                year = history.Year,
                totalPoints = history.TotalPoints,
                maxPossiblePoints = history.MaxPossiblePoints,
                picks = history.Picks.Select(p => new
                {
                    gameNumber = p.GameNumber,
                    bowlName = p.BowlName,
                    favorite = p.Favorite,
                    underdog = p.Underdog,
                    line = p.Line,
                    spreadPick = p.SpreadPick,
                    confidencePoints = p.ConfidencePoints,
                    outrightWinnerPick = p.OutrightWinnerPick,
                    hasResult = p.HasResult,
                    favoriteScore = p.FavoriteScore,
                    underdogScore = p.UnderdogScore,
                    spreadWinner = p.SpreadWinner,
                    isPush = p.IsPush,
                    actualOutrightWinner = p.ActualOutrightWinner,
                    pointsEarned = p.PointsEarned,
                    spreadPickCorrect = p.SpreadPickCorrect,
                    outrightPickCorrect = p.OutrightPickCorrect
                })
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bowl history");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "Failed to get bowl history" });
            return errorResponse;
        }
    }

    /// <summary>
    /// Get current user's bowl history for a year.
    /// GET /api/my-bowl-history?year={year}
    /// </summary>
    [Function("GetMyBowlHistory")]
    public async Task<HttpResponseData> GetMyBowlHistory(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "my-bowl-history")] HttpRequestData req)
    {
        _logger.LogInformation("Processing get my bowl history request");

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
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteAsJsonAsync(new { error = "User not found" });
                return notFoundResponse;
            }

            // Get user history
            var history = await _bowlLeaderboardService.GetUserBowlHistoryAsync(user.Id, year);

            if (history == null)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteAsJsonAsync(new { error = "No bowl picks found for this year" });
                return notFoundResponse;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                userId = history.UserId,
                displayName = history.DisplayName,
                year = history.Year,
                totalPoints = history.TotalPoints,
                maxPossiblePoints = history.MaxPossiblePoints,
                picks = history.Picks.Select(p => new
                {
                    gameNumber = p.GameNumber,
                    bowlName = p.BowlName,
                    favorite = p.Favorite,
                    underdog = p.Underdog,
                    line = p.Line,
                    spreadPick = p.SpreadPick,
                    confidencePoints = p.ConfidencePoints,
                    outrightWinnerPick = p.OutrightWinnerPick,
                    hasResult = p.HasResult,
                    favoriteScore = p.FavoriteScore,
                    underdogScore = p.UnderdogScore,
                    spreadWinner = p.SpreadWinner,
                    isPush = p.IsPush,
                    actualOutrightWinner = p.ActualOutrightWinner,
                    pointsEarned = p.PointsEarned,
                    spreadPickCorrect = p.SpreadPickCorrect,
                    outrightPickCorrect = p.OutrightPickCorrect
                })
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting my bowl history");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "Failed to get bowl history" });
            return errorResponse;
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

    #endregion
}
