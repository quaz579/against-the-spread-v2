using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Functions.Helpers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace AgainstTheSpread.Functions;

/// <summary>
/// Azure Function for uploading bowl game lines
/// </summary>
public class UploadBowlLinesFunction
{
    private readonly ILogger<UploadBowlLinesFunction> _logger;
    private readonly IBowlExcelService _bowlExcelService;
    private readonly IStorageService _storageService;
    private readonly AuthHelper _authHelper;

    public UploadBowlLinesFunction(
        ILogger<UploadBowlLinesFunction> logger,
        IBowlExcelService bowlExcelService,
        IStorageService storageService)
    {
        _logger = logger;
        _bowlExcelService = bowlExcelService;
        _storageService = storageService;
        _authHelper = new AuthHelper(logger);
    }

    /// <summary>
    /// Upload bowl lines file
    /// POST /api/upload-bowl-lines
    /// </summary>
    [Function("UploadBowlLines")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "upload-bowl-lines")] HttpRequestData req)
    {
        _logger.LogInformation("Processing bowl lines upload request");

        try
        {
            // Check authentication and authorization using AuthHelper
            var (user, errorResponse) = await ValidateAdminAccessAsync(req);
            if (errorResponse != null)
            {
                return errorResponse;
            }

            // Get year from query parameters
            if (!int.TryParse(req.Query["year"], out int year))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Year parameter is required" });
                return badResponse;
            }

            // Read the file from request body
            // Note: When receiving multipart form data, the body stream contains the file content
            using var stream = new MemoryStream();
            await req.Body.CopyToAsync(stream);
            stream.Position = 0;

            if (stream.Length == 0)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "No file uploaded" });
                return badResponse;
            }

            _logger.LogInformation("Uploading bowl lines for year {Year}, file size: {Size} bytes",
                year, stream.Length);

            // Read and validate the Excel file
            var bowlLines = await _bowlExcelService.ParseBowlLinesAsync(stream, year);

            if (bowlLines.Games.Count == 0)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "No games found in the uploaded file" });
                return badResponse;
            }

            // Upload to blob storage
            stream.Position = 0; // Reset stream
            await _storageService.UploadBowlLinesAsync(stream, year);

            _logger.LogInformation("Successfully uploaded {Count} bowl games for year {Year}",
                bowlLines.Games.Count, year);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                success = true,
                year = year,
                gamesCount = bowlLines.Games.Count,
                message = $"Successfully uploaded {bowlLines.Games.Count} bowl games for {year}"
            });
            return response;
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(ex, "Format error uploading bowl lines");
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(new { error = ex.Message });
            return badResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading bowl lines");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "Failed to upload bowl lines" });
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
