using AgainstTheSpread.Data.Interfaces;
using AgainstTheSpread.Functions.Helpers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace AgainstTheSpread.Functions;

/// <summary>
/// Azure Function for managing user picks.
/// Provides endpoints for submitting and retrieving picks.
/// </summary>
public class UserPicksFunction
{
    private readonly ILogger<UserPicksFunction> _logger;
    private readonly IPickService _pickService;
    private readonly IUserService _userService;
    private readonly AuthHelper _authHelper;

    public UserPicksFunction(
        ILogger<UserPicksFunction> logger,
        IPickService pickService,
        IUserService userService)
    {
        _logger = logger;
        _pickService = pickService;
        _userService = userService;
        _authHelper = new AuthHelper(logger);
    }

    /// <summary>
    /// Submit or update picks for a user.
    /// POST /api/user-picks
    /// </summary>
    [Function("SubmitUserPicks")]
    public async Task<HttpResponseData> SubmitPicks(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "user-picks")] HttpRequestData req)
    {
        _logger.LogInformation("Processing pick submission request");

        try
        {
            // Validate authentication
            var authResult = ValidateAndGetUser(req);
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

            var submitRequest = JsonSerializer.Deserialize<SubmitPicksRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (submitRequest == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request format");
            }

            if (submitRequest.Year <= 0 || submitRequest.Week <= 0)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Year and Week are required");
            }

            if (submitRequest.Picks == null || !submitRequest.Picks.Any())
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "At least one pick is required");
            }

            // Convert to service model
            var picks = submitRequest.Picks.Select(p => new PickSubmission(p.GameId, p.SelectedTeam)).ToList();

            // Submit picks
            var result = await _pickService.SubmitPicksAsync(user.Id, submitRequest.Year, submitRequest.Week, picks);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                success = result.Success,
                picksSubmitted = result.PicksSubmitted,
                picksRejected = result.PicksRejected,
                rejectedPicks = result.RejectedPicks.Select(r => new { gameId = r.GameId, reason = r.Reason }),
                message = result.Success
                    ? $"Successfully submitted {result.PicksSubmitted} picks"
                    : result.ErrorMessage
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting picks");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Failed to submit picks");
        }
    }

    /// <summary>
    /// Get user's picks for a specific week.
    /// GET /api/user-picks/{week}?year={year}
    /// </summary>
    [Function("GetUserWeekPicks")]
    public async Task<HttpResponseData> GetWeekPicks(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "user-picks/{week:int}")] HttpRequestData req,
        int week)
    {
        _logger.LogInformation("Processing get week picks request for week {Week}", week);

        try
        {
            // Validate authentication
            var authResult = ValidateAndGetUser(req);
            if (authResult.ErrorResponse != null)
            {
                return authResult.ErrorResponse;
            }

            // Get year from query string
            if (!int.TryParse(req.Query["year"], out int year))
            {
                year = DateTime.Now.Year;
            }

            // Get user from database
            var user = await _userService.GetByGoogleSubjectIdAsync(authResult.UserInfo!.UserId);
            if (user == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.NotFound, "User not found");
            }

            // Get picks
            var picks = await _pickService.GetUserPicksAsync(user.Id, year, week);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                year = year,
                week = week,
                picks = picks.Select(p => new
                {
                    gameId = p.GameId,
                    selectedTeam = p.SelectedTeam,
                    submittedAt = p.SubmittedAt,
                    updatedAt = p.UpdatedAt,
                    game = p.Game != null ? new
                    {
                        id = p.Game.Id,
                        favorite = p.Game.Favorite,
                        underdog = p.Game.Underdog,
                        line = p.Game.Line,
                        gameDate = p.Game.GameDate,
                        isLocked = p.Game.IsLocked,
                        hasResult = p.Game.HasResult,
                        spreadWinner = p.Game.SpreadWinner,
                        isPush = p.Game.IsPush
                    } : null
                })
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting week picks");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Failed to get picks");
        }
    }

    /// <summary>
    /// Get user's picks for entire season.
    /// GET /api/user-picks?year={year}
    /// </summary>
    [Function("GetUserSeasonPicks")]
    public async Task<HttpResponseData> GetSeasonPicks(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "user-picks")] HttpRequestData req)
    {
        _logger.LogInformation("Processing get season picks request");

        try
        {
            // Validate authentication
            var authResult = ValidateAndGetUser(req);
            if (authResult.ErrorResponse != null)
            {
                return authResult.ErrorResponse;
            }

            // Get year from query string
            if (!int.TryParse(req.Query["year"], out int year))
            {
                year = DateTime.Now.Year;
            }

            // Get user from database
            var user = await _userService.GetByGoogleSubjectIdAsync(authResult.UserInfo!.UserId);
            if (user == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.NotFound, "User not found");
            }

            // Get picks
            var picks = await _pickService.GetUserSeasonPicksAsync(user.Id, year);

            // Group by week
            var picksByWeek = picks
                .GroupBy(p => p.Week)
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    week = g.Key,
                    picks = g.Select(p => new
                    {
                        gameId = p.GameId,
                        selectedTeam = p.SelectedTeam,
                        submittedAt = p.SubmittedAt,
                        updatedAt = p.UpdatedAt,
                        game = p.Game != null ? new
                        {
                            id = p.Game.Id,
                            favorite = p.Game.Favorite,
                            underdog = p.Game.Underdog,
                            line = p.Game.Line,
                            gameDate = p.Game.GameDate,
                            isLocked = p.Game.IsLocked,
                            hasResult = p.Game.HasResult,
                            spreadWinner = p.Game.SpreadWinner,
                            isPush = p.Game.IsPush
                        } : null
                    })
                });

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                year = year,
                totalPicks = picks.Count,
                weeks = picksByWeek
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting season picks");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Failed to get picks");
        }
    }

    #region Helper Methods

    private (UserInfo? UserInfo, HttpResponseData? ErrorResponse) ValidateAndGetUser(HttpRequestData req)
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
            response.WriteStringAsync(authResult.Error ?? "Authentication failed").Wait();
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
/// Request model for submitting picks.
/// </summary>
public class SubmitPicksRequest
{
    public int Year { get; set; }
    public int Week { get; set; }
    public List<PickInput>? Picks { get; set; }
}

/// <summary>
/// Individual pick input in a submission request.
/// </summary>
public class PickInput
{
    public int GameId { get; set; }
    public string SelectedTeam { get; set; } = string.Empty;
}

#endregion
