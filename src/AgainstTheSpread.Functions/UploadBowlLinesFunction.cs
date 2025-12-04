using AgainstTheSpread.Core.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
using System.Text.Json;

namespace AgainstTheSpread.Functions;

/// <summary>
/// Azure Function for uploading bowl game lines
/// </summary>
public class UploadBowlLinesFunction
{
    private readonly ILogger<UploadBowlLinesFunction> _logger;
    private readonly IBowlExcelService _bowlExcelService;
    private readonly IStorageService _storageService;

    public UploadBowlLinesFunction(
        ILogger<UploadBowlLinesFunction> logger,
        IBowlExcelService bowlExcelService,
        IStorageService storageService)
    {
        _logger = logger;
        _bowlExcelService = bowlExcelService;
        _storageService = storageService;
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
            // Check authentication and authorization
            if (!IsAuthorized(req, out var errorResponse))
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
            var principal = JsonSerializer.Deserialize<BowlClientPrincipal>(principalJson, new JsonSerializerOptions
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
                // Try UserDetails property - Google OAuth in SWA often stores email here
                userEmail = principal.UserDetails;
                _logger.LogInformation("Email retrieved from UserDetails for user {UserId}", principal.UserId);
            }

            if (string.IsNullOrEmpty(userEmail))
            {
                _logger.LogWarning("No email found in claims or UserDetails for user {UserId}. Claims: {Claims}", 
                    principal.UserId, 
                    principal.Claims?.Select(c => $"{c.Type}={c.Value}").ToList() ?? new List<string>());
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

    /// <summary>
    /// Represents the client principal from SWA authentication for bowl functions
    /// </summary>
    private class BowlClientPrincipal
    {
        public string? IdentityProvider { get; set; }
        public string? UserId { get; set; }
        public string? UserDetails { get; set; }
        public List<string>? UserRoles { get; set; }
        public List<BowlClientPrincipalClaim>? Claims { get; set; }
    }

    /// <summary>
    /// Represents a claim in the client principal for bowl functions
    /// </summary>
    private class BowlClientPrincipalClaim
    {
        public string? Type { get; set; }
        public string? Value { get; set; }
    }
}
