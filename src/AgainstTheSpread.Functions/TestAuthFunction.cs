using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace AgainstTheSpread.Functions;

/// <summary>
/// Test authentication function for E2E testing.
/// Only active when ENABLE_TEST_AUTH environment variable is set to "true".
/// This should NEVER be enabled in production environments.
/// </summary>
public class TestAuthFunction
{
    private readonly ILogger<TestAuthFunction> _logger;

    public TestAuthFunction(ILogger<TestAuthFunction> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// GET /api/test-auth/me
    /// Mock endpoint for testing that mimics SWA's /.auth/me response.
    /// Returns user info based on X-Test-User-Email header.
    /// </summary>
    [Function("TestAuthMe")]
    public async Task<HttpResponseData> GetTestAuthMe(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "test-auth/me")] HttpRequestData req)
    {
        // Only respond if test auth is enabled
        if (!IsTestAuthEnabled())
        {
            _logger.LogWarning("Test auth endpoint called but ENABLE_TEST_AUTH is not enabled");
            var errorResponse = req.CreateResponse(HttpStatusCode.NotFound);
            await errorResponse.WriteAsJsonAsync(new { error = "Test auth is not enabled" });
            return errorResponse;
        }

        // Get test email from header
        if (!req.Headers.TryGetValues("X-Test-User-Email", out var testEmailValues))
        {
            _logger.LogWarning("Test auth endpoint called without X-Test-User-Email header");
            var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await errorResponse.WriteAsJsonAsync(new { error = "X-Test-User-Email header required" });
            return errorResponse;
        }

        var testEmail = testEmailValues.FirstOrDefault();
        if (string.IsNullOrEmpty(testEmail))
        {
            var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await errorResponse.WriteAsJsonAsync(new { error = "X-Test-User-Email header is empty" });
            return errorResponse;
        }

        _logger.LogInformation("Test auth: returning mock user for {Email}", testEmail);

        // Return response in SWA /.auth/me format
        var response = req.CreateResponse(HttpStatusCode.OK);
        var authResponse = new
        {
            clientPrincipal = new
            {
                identityProvider = "test",
                userId = $"test-{testEmail.GetHashCode():X8}",
                userDetails = testEmail,
                userRoles = new[] { "authenticated", "anonymous" },
                claims = new[]
                {
                    new { typ = "email", val = testEmail },
                    new { typ = "name", val = testEmail.Split('@')[0] }
                }
            }
        };

        await response.WriteAsJsonAsync(authResponse);
        return response;
    }

    private static bool IsTestAuthEnabled() =>
        Environment.GetEnvironmentVariable("ENABLE_TEST_AUTH")?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
}
