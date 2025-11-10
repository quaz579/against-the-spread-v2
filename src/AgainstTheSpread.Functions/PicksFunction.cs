using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Core.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace AgainstTheSpread.Functions;

/// <summary>
/// API endpoint for submitting user picks and downloading Excel
/// </summary>
public class PicksFunction
{
    private readonly ILogger<PicksFunction> _logger;
    private readonly IExcelService _excelService;

    public PicksFunction(ILogger<PicksFunction> logger, IExcelService excelService)
    {
        _logger = logger;
        _excelService = excelService;
    }

    /// <summary>
    /// POST /api/picks
    /// Accepts user picks and returns Excel file in exact format
    /// </summary>
    [Function("SubmitPicks")]
    public async Task<HttpResponseData> SubmitPicks(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "picks")] HttpRequestData req)
    {
        _logger.LogInformation("Processing SubmitPicks request");

        try
        {
            // Parse request body
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var userPicks = JsonSerializer.Deserialize<UserPicks>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (userPicks == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Invalid request body" });
                return badResponse;
            }

            // Set submission time
            userPicks.SubmittedAt = DateTime.UtcNow;

            // Validate picks
            if (!userPicks.IsValid())
            {
                var validationError = userPicks.GetValidationError();
                var validationResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await validationResponse.WriteAsJsonAsync(new { error = validationError });
                return validationResponse;
            }

            // Generate Excel file
            var excelBytes = await _excelService.GeneratePicksExcelAsync(userPicks);

            // Return Excel file
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            response.Headers.Add("Content-Disposition",
                $"attachment; filename=\"{userPicks.Name.Replace(" ", "_")}_Week_{userPicks.Week}_Picks.xlsx\"");
            await response.Body.WriteAsync(excelBytes);
            return response;
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error in SubmitPicks");
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(new { error = ex.Message });
            return badResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing picks submission");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "Failed to generate picks file" });
            return errorResponse;
        }
    }
}
