using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Data.Interfaces;
using AgainstTheSpread.Functions.Helpers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace AgainstTheSpread.Functions;

/// <summary>
/// Azure Function for uploading weekly game lines.
/// Syncs to database as primary storage, archives to blob storage.
/// </summary>
public class UploadLinesFunction
{
    private readonly ILogger<UploadLinesFunction> _logger;
    private readonly IExcelService _excelService;
    private readonly IArchiveService _archiveService;
    private readonly IGameService _gameService;
    private readonly AuthHelper _authHelper;

    public UploadLinesFunction(
        ILogger<UploadLinesFunction> logger,
        IExcelService excelService,
        IArchiveService archiveService,
        IGameService gameService)
    {
        _logger = logger;
        _excelService = excelService;
        _archiveService = archiveService;
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

            // Sync games to database (primary storage)
            var gameInputs = weeklyLines.Games.Select(g => new GameSyncInput(
                g.Favorite,
                g.Underdog,
                g.Line,
                g.GameDate));

            var gamesSynced = await _gameService.SyncGamesFromLinesAsync(year, week, gameInputs);
            _logger.LogInformation("Synced {Count} games to database for week {Week}", gamesSynced, week);

            // Archive to blob storage (optional - don't fail if archive fails)
            string? archiveWarning = null;
            try
            {
                stream.Position = 0; // Reset stream
                await _archiveService.ArchiveWeeklyLinesAsync(stream, week, year);
                _logger.LogInformation("Archived Excel file to blob storage for week {Week}", week);
            }
            catch (Exception archiveEx)
            {
                _logger.LogWarning(archiveEx, "Failed to archive Excel file to blob storage for week {Week}. Database sync succeeded.", week);
                archiveWarning = "Games synced to database but Excel archive failed. Games are available.";
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
                warning = archiveWarning
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
