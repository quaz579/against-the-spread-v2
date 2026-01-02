using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Data.Interfaces;
using AgainstTheSpread.Functions.Helpers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace AgainstTheSpread.Functions;

/// <summary>
/// Azure Function for syncing sports data from external API.
/// Admin-only endpoints for pulling games and results.
/// </summary>
public class SportsDataSyncFunction
{
    private readonly ILogger<SportsDataSyncFunction> _logger;
    private readonly ISportsDataProvider? _sportsDataProvider;
    private readonly IGameService _gameService;
    private readonly IBowlGameService _bowlGameService;
    private readonly IUserService _userService;
    private readonly IGameResultMatcher _gameResultMatcher;
    private readonly IResultService _resultService;
    private readonly AuthHelper _authHelper;

    public SportsDataSyncFunction(
        ILogger<SportsDataSyncFunction> logger,
        IGameService gameService,
        IBowlGameService bowlGameService,
        IUserService userService,
        IGameResultMatcher gameResultMatcher,
        IResultService resultService,
        ISportsDataProvider? sportsDataProvider = null)
    {
        _logger = logger;
        _sportsDataProvider = sportsDataProvider;
        _gameService = gameService;
        _bowlGameService = bowlGameService;
        _userService = userService;
        _gameResultMatcher = gameResultMatcher;
        _resultService = resultService;
        _authHelper = new AuthHelper(logger);
    }

    /// <summary>
    /// Sync weekly games from external API.
    /// POST /api/sync/games/{week}?year={year}
    /// Admin only.
    /// </summary>
    [Function("SyncWeeklyGames")]
    public async Task<HttpResponseData> SyncWeeklyGames(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sync/games/{week:int}")] HttpRequestData req,
        int week)
    {
        _logger.LogInformation("Processing sync weekly games request for week {Week}", week);

        try
        {
            // Validate admin access
            var authResult = await ValidateAdminAccessAsync(req);
            if (authResult.ErrorResponse != null)
            {
                return authResult.ErrorResponse;
            }

            // Check if provider is configured
            if (_sportsDataProvider == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.ServiceUnavailable,
                    "Sports data provider not configured. Please set CFBD_API_KEY environment variable.");
            }

            // Get year from query string
            if (!int.TryParse(req.Query["year"], out int year))
            {
                year = DateTime.UtcNow.Year;
            }

            // Fetch games from external API
            var externalGames = await _sportsDataProvider.GetWeeklyGamesAsync(year, week);

            if (!externalGames.Any())
            {
                var emptyResponse = req.CreateResponse(HttpStatusCode.OK);
                await emptyResponse.WriteAsJsonAsync(new
                {
                    success = true,
                    message = "No games found for the specified week",
                    gamesSynced = 0
                });
                return emptyResponse;
            }

            // Convert to internal format and sync to database
            var gameSyncInputs = externalGames.Select(g => new GameSyncInput(
                g.Favorite,
                g.Underdog,
                g.Line,
                g.GameDate
            )).ToList();

            var syncedCount = await _gameService.SyncGamesFromLinesAsync(year, week, gameSyncInputs);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                success = true,
                message = $"Successfully synced {syncedCount} games from {_sportsDataProvider.ProviderName}",
                provider = _sportsDataProvider.ProviderName,
                year = year,
                week = week,
                gamesSynced = syncedCount,
                gamesFound = externalGames.Count
            });

            return response;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error syncing games for week {Week}", week);
            return await CreateErrorResponse(req, HttpStatusCode.BadGateway,
                $"Error communicating with sports data provider: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing games for week {Week}", week);
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError,
                "Failed to sync games from external API");
        }
    }

    /// <summary>
    /// Sync bowl games from external API.
    /// POST /api/sync/bowl-games?year={year}
    /// Admin only.
    /// </summary>
    [Function("SyncBowlGames")]
    public async Task<HttpResponseData> SyncBowlGames(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sync/bowl-games")] HttpRequestData req)
    {
        _logger.LogInformation("Processing sync bowl games request");

        try
        {
            // Validate admin access
            var authResult = await ValidateAdminAccessAsync(req);
            if (authResult.ErrorResponse != null)
            {
                return authResult.ErrorResponse;
            }

            // Check if provider is configured
            if (_sportsDataProvider == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.ServiceUnavailable,
                    "Sports data provider not configured. Please set CFBD_API_KEY environment variable.");
            }

            // Get year from query string
            if (!int.TryParse(req.Query["year"], out int year))
            {
                year = DateTime.UtcNow.Year;
            }

            // Fetch bowl games from external API
            var externalGames = await _sportsDataProvider.GetBowlGamesAsync(year);

            if (!externalGames.Any())
            {
                var emptyResponse = req.CreateResponse(HttpStatusCode.OK);
                await emptyResponse.WriteAsJsonAsync(new
                {
                    success = true,
                    message = "No bowl games found for the specified year",
                    gamesSynced = 0
                });
                return emptyResponse;
            }

            // Convert to internal format and sync to database
            var gameNumber = 1;
            var gameSyncInputs = externalGames
                .OrderBy(g => g.GameDate)
                .Select(g => new BowlGameSyncInput(
                    gameNumber++,
                    g.BowlName,
                    g.Favorite,
                    g.Underdog,
                    g.Line,
                    g.GameDate
                )).ToList();

            var syncedCount = await _bowlGameService.SyncBowlGamesAsync(year, gameSyncInputs);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                success = true,
                message = $"Successfully synced {syncedCount} bowl games from {_sportsDataProvider.ProviderName}",
                provider = _sportsDataProvider.ProviderName,
                year = year,
                gamesSynced = syncedCount,
                gamesFound = externalGames.Count
            });

            return response;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error syncing bowl games");
            return await CreateErrorResponse(req, HttpStatusCode.BadGateway,
                $"Error communicating with sports data provider: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing bowl games");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError,
                "Failed to sync bowl games from external API");
        }
    }

    /// <summary>
    /// Sync weekly game results from external API.
    /// POST /api/sync/results/{week}?year={year}
    /// Admin only.
    /// </summary>
    [Function("SyncWeeklyResults")]
    public async Task<HttpResponseData> SyncWeeklyResults(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sync/results/{week:int}")] HttpRequestData req,
        int week)
    {
        _logger.LogInformation("Processing sync weekly results request for week {Week}", week);

        try
        {
            // Validate admin access
            var authResult = await ValidateAdminAccessAsync(req);
            if (authResult.ErrorResponse != null)
            {
                return authResult.ErrorResponse;
            }

            // Check if provider is configured
            if (_sportsDataProvider == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.ServiceUnavailable,
                    "Sports data provider not configured. Please set CFBD_API_KEY environment variable.");
            }

            // Get year from query string
            if (!int.TryParse(req.Query["year"], out int year))
            {
                year = DateTime.UtcNow.Year;
            }

            // Fetch results from external API
            var externalResults = await _sportsDataProvider.GetWeeklyResultsAsync(year, week);

            if (!externalResults.Any())
            {
                var emptyResponse = req.CreateResponse(HttpStatusCode.OK);
                await emptyResponse.WriteAsJsonAsync(new
                {
                    success = true,
                    message = "No completed games found for the specified week",
                    resultsSynced = 0,
                    gamesNotFound = 0,
                    gamesSkipped = 0
                });
                return emptyResponse;
            }

            // Match external results to database games
            var matchResult = await _gameResultMatcher.MatchResultsToGamesAsync(year, week, externalResults);

            // Get or create admin user
            var adminUser = await _userService.GetOrCreateUserAsync(
                authResult.UserInfo!.UserId,
                authResult.UserInfo.Email,
                authResult.UserInfo.DisplayName ?? authResult.UserInfo.Email);

            // Enter results for matched games
            var resultsToEnter = matchResult.Matched.Select(m =>
                new GameResultInput(m.GameId, m.FavoriteScore, m.UnderdogScore)).ToList();

            var bulkResult = await _resultService.BulkEnterResultsAsync(
                year, week, resultsToEnter, adminUser.Id);

            // Build response
            var unmatchedList = matchResult.Unmatched
                .Select(u => $"{u.HomeTeam} vs {u.AwayTeam}")
                .ToList();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                success = true,
                message = $"Synced {bulkResult.ResultsEntered} results from {_sportsDataProvider.ProviderName}",
                provider = _sportsDataProvider.ProviderName,
                year = year,
                week = week,
                resultsSynced = bulkResult.ResultsEntered,
                gamesNotFound = matchResult.Unmatched.Count,
                gamesSkipped = matchResult.GamesWithExistingResults,
                unmatchedGames = unmatchedList.Any() ? unmatchedList : null
            });

            return response;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error syncing results for week {Week}", week);
            return await CreateErrorResponse(req, HttpStatusCode.BadGateway,
                $"Error communicating with sports data provider: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing results for week {Week}", week);
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError,
                "Failed to sync results from external API");
        }
    }

    /// <summary>
    /// Get available data providers info.
    /// GET /api/sync/status
    /// </summary>
    [Function("GetSyncStatus")]
    public async Task<HttpResponseData> GetSyncStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "sync/status")] HttpRequestData req)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            provider = _sportsDataProvider?.ProviderName ?? "Not configured",
            isConfigured = _sportsDataProvider != null,
            message = _sportsDataProvider != null
                ? $"Sports data provider '{_sportsDataProvider.ProviderName}' is ready"
                : "No sports data provider configured. Set CFBD_API_KEY to enable API sync."
        });

        return response;
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
        await response.WriteAsJsonAsync(new { success = false, message = message });
        return response;
    }

    #endregion
}
