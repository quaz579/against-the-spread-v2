using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Data.Interfaces;
using AgainstTheSpread.Functions.Helpers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace AgainstTheSpread.Functions;

/// <summary>
/// Azure Function for uploading weekly game lines
/// </summary>
public class UploadLinesFunction
{
    private readonly ILogger<UploadLinesFunction> _logger;
    private readonly IExcelService _excelService;
    private readonly IStorageService _storageService;
    private readonly IGameService? _gameService;
    private readonly AuthHelper _authHelper;

    public UploadLinesFunction(
        ILogger<UploadLinesFunction> logger,
        IExcelService excelService,
        IStorageService storageService,
        IGameService? gameService = null)
    {
        _logger = logger;
        _excelService = excelService;
        _storageService = storageService;
        _gameService = gameService;
        _authHelper = new AuthHelper(logger);
    }

    /// <summary>
    /// Upload weekly lines file
    /// POST /api/upload-lines
    /// </summary>
    [Function("UploadLines")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "upload-lines")] HttpRequestData req)
    {
        _logger.LogInformation("Processing upload request");

        try
        {
            // Check authentication and authorization
            var authResult = await ValidateAdminAccessAsync(req);
            if (authResult.ErrorResponse != null)
            {
                return authResult.ErrorResponse;
            }

            // Get week and year from query parameters
            if (!int.TryParse(req.Query["week"], out int week))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Week parameter is required" });
                return badResponse;
            }

            if (!int.TryParse(req.Query["year"], out int year))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Year parameter is required" });
                return badResponse;
            }

            // Read the file from request body (expecting raw file upload)
            using var stream = new MemoryStream();
            await req.Body.CopyToAsync(stream);
            stream.Position = 0;

            if (stream.Length == 0)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "No file uploaded" });
                return badResponse;
            }

            _logger.LogInformation("Uploading week {Week} for year {Year}, file size: {Size} bytes",
                week, year, stream.Length);

            // Read and validate the Excel file
            var weeklyLines = await _excelService.ParseWeeklyLinesAsync(stream, week, year);

            if (weeklyLines.Games.Count == 0)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "No games found in the uploaded file" });
                return badResponse;
            }

            // Upload to blob storage
            stream.Position = 0; // Reset stream
            await _storageService.UploadWeeklyLinesAsync(stream, week, year);

            _logger.LogInformation("Successfully uploaded {Count} games for week {Week}",
                weeklyLines.Games.Count, week);

            // Sync games to database if service is available (graceful - don't fail upload if DB sync fails)
            var gamesSynced = 0;
            string? dbSyncWarning = null;
            if (_gameService != null)
            {
                try
                {
                    var gameInputs = weeklyLines.Games.Select(g => new GameSyncInput(
                        g.Favorite,
                        g.Underdog,
                        g.Line,
                        g.GameDate));

                    gamesSynced = await _gameService.SyncGamesFromLinesAsync(year, week, gameInputs);
                    _logger.LogInformation("Synced {Count} games to database for week {Week}", gamesSynced, week);
                }
                catch (Exception dbEx)
                {
                    _logger.LogWarning(dbEx, "Failed to sync games to database for week {Week}. Upload to blob storage succeeded.", week);
                    dbSyncWarning = "Games uploaded to storage but database sync failed. Games will still be available.";
                }
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                success = true,
                week = week,
                year = year,
                gamesCount = weeklyLines.Games.Count,
                gamesSynced = gamesSynced,
                message = $"Successfully uploaded {weeklyLines.Games.Count} games for Week {week}",
                warning = dbSyncWarning
            });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading lines");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "Failed to upload lines" });
            return errorResponse;
        }
    }

    /// <summary>
    /// Validates admin access using AuthHelper.
    /// </summary>
    private async Task<(UserInfo? UserInfo, HttpResponseData? ErrorResponse)> ValidateAdminAccessAsync(HttpRequestData req)
    {
        // Convert headers to dictionary for AuthHelper
        var headers = req.Headers.ToDictionary(
            h => h.Key,
            h => h.Value,
            StringComparer.OrdinalIgnoreCase);

        // Validate authentication
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

        // Check if user is admin
        if (!_authHelper.IsAdmin(authResult.User))
        {
            var response = req.CreateResponse(HttpStatusCode.Forbidden);
            await response.WriteStringAsync("Access denied. Admin privileges required.");
            return (null, response);
        }

        _logger.LogInformation("Admin access granted for {Email}", authResult.User.Email);
        return (authResult.User, null);
    }
}
