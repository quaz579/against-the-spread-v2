using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Core.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace AgainstTheSpread.Functions;

/// <summary>
/// API endpoint for submitting bowl picks and downloading Excel.
/// </summary>
public class BowlPicksFunction
{
    private readonly ILogger<BowlPicksFunction> _logger;
    private readonly IBowlExcelService _bowlExcelService;

    public BowlPicksFunction(ILogger<BowlPicksFunction> logger, IBowlExcelService bowlExcelService)
    {
        _logger = logger;
        _bowlExcelService = bowlExcelService;
    }

    /// <summary>
    /// POST /api/bowl-picks
    /// Accepts bowl picks and returns Excel file in required format
    /// </summary>
    [Function("SubmitBowlPicks")]
    public async Task<HttpResponseData> SubmitBowlPicks(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "bowl-picks")] HttpRequestData req)
    {
        _logger.LogInformation("Processing SubmitBowlPicks request");

        try
        {
            string body;
            using (var reader = new StreamReader(req.Body))
            {
                body = await reader.ReadToEndAsync();
            }
            var userPicks = JsonSerializer.Deserialize<BowlUserPicks>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (userPicks == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Invalid request body" });
                return badResponse;
            }

            userPicks.SubmittedAt = DateTime.UtcNow;

            if (!userPicks.IsValid())
            {
                var validationError = userPicks.GetValidationError();
                var validationResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await validationResponse.WriteAsJsonAsync(new { error = validationError });
                return validationResponse;
            }

            var excelBytes = await _bowlExcelService.GenerateBowlPicksExcelAsync(userPicks);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            response.Headers.Add("Content-Disposition",
                $"attachment; filename=\"{userPicks.Name.Replace(" ", "_")}_Bowl_Picks_{userPicks.Year}.xlsx\"");
            await response.Body.WriteAsync(excelBytes);
            return response;
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error in SubmitBowlPicks");
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(new { error = ex.Message });
            return badResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing bowl picks submission");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "Failed to generate bowl picks file" });
            return errorResponse;
        }
    }
}
