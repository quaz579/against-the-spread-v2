using AgainstTheSpread.Core.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
using System.Text.Json;

namespace AgainstTheSpread.Functions;

/// <summary>
/// Azure Function for uploading weekly game lines
/// </summary>
public class UploadLinesFunction
{
    private readonly ILogger<UploadLinesFunction> _logger;
    private readonly IExcelService _excelService;
    private readonly IStorageService _storageService;

    public UploadLinesFunction(
        ILogger<UploadLinesFunction> logger,
        IExcelService excelService,
        IStorageService storageService)
    {
        _logger = logger;
        _excelService = excelService;
        _storageService = storageService;
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
            if (!IsAuthorized(req, out var errorResponse))
            {
                return errorResponse;
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

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                success = true,
                week = week,
                year = year,
                gamesCount = weeklyLines.Games.Count,
                message = $"Successfully uploaded {weeklyLines.Games.Count} games for Week {week}"
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
    /// Check if the request is from an authorized admin user
    /// </summary>
    private bool IsAuthorized(HttpRequestData req, out HttpResponseData errorResponse)
    {
        errorResponse = null!;

        // Get the client principal from SWA auth header
        if (!req.Headers.TryGetValues("X-MS-CLIENT-PRINCIPAL", out var principalValues))
        {
            _logger.LogWarning("No authentication header found");
            errorResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
            errorResponse.WriteStringAsync("Authentication required").Wait();
            return false;
        }

        var principalHeader = principalValues.FirstOrDefault();
        if (string.IsNullOrEmpty(principalHeader))
        {
            _logger.LogWarning("Empty authentication header");
            errorResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
            errorResponse.WriteStringAsync("Authentication required").Wait();
            return false;
        }

        try
        {
            // Decode the base64-encoded principal
            var principalJson = Encoding.UTF8.GetString(Convert.FromBase64String(principalHeader));
            var principal = JsonSerializer.Deserialize<ClientPrincipal>(principalJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (principal == null || string.IsNullOrEmpty(principal.UserId))
            {
                _logger.LogWarning("Invalid principal data");
                errorResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                errorResponse.WriteStringAsync("Invalid authentication").Wait();
                return false;
            }

            // Get admin email list from environment variable
            var adminEmailsConfig = Environment.GetEnvironmentVariable("ADMIN_EMAILS") ?? "";
            var adminEmails = adminEmailsConfig
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(e => e.ToLowerInvariant())
                .ToHashSet();

            // Get user email from claims
            var userEmail = principal.Claims
                ?.FirstOrDefault(c => c.Type?.Equals("email", StringComparison.OrdinalIgnoreCase) == true)
                ?.Value;

            if (string.IsNullOrEmpty(userEmail))
            {
                // Try alternative claim types
                userEmail = principal.Claims
                    ?.FirstOrDefault(c => c.Type?.Equals("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", StringComparison.OrdinalIgnoreCase) == true)
                    ?.Value;
            }

            if (string.IsNullOrEmpty(userEmail))
            {
                _logger.LogWarning("No email claim found for user {UserId}", principal.UserId);
                errorResponse = req.CreateResponse(HttpStatusCode.Forbidden);
                errorResponse.WriteStringAsync("Email not found in authentication").Wait();
                return false;
            }

            // Check if user email is in admin list
            if (!adminEmails.Contains(userEmail.ToLowerInvariant()))
            {
                _logger.LogWarning("User {Email} is not authorized as admin", userEmail);
                errorResponse = req.CreateResponse(HttpStatusCode.Forbidden);
                errorResponse.WriteStringAsync("Access denied. Admin privileges required.").Wait();
                return false;
            }

            _logger.LogInformation("Admin access granted for {Email}", userEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating authentication");
            errorResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
            errorResponse.WriteStringAsync("Authentication validation failed").Wait();
            return false;
        }
    }
}

/// <summary>
/// Represents the client principal from SWA authentication
/// </summary>
internal class ClientPrincipal
{
    public string? IdentityProvider { get; set; }
    public string? UserId { get; set; }
    public string? UserDetails { get; set; }
    public List<string>? UserRoles { get; set; }
    public List<ClientPrincipalClaim>? Claims { get; set; }
}

/// <summary>
/// Represents a claim in the client principal
/// </summary>
internal class ClientPrincipalClaim
{
    public string? Type { get; set; }
    public string? Value { get; set; }
}
