using AgainstTheSpread.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

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
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "upload-lines")] HttpRequest req)
    {
        _logger.LogInformation("Processing upload request");

        try
        {
            // Get week and year from query parameters
            if (!int.TryParse(req.Query["week"], out int week))
            {
                return new BadRequestObjectResult(new { error = "Week parameter is required" });
            }

            if (!int.TryParse(req.Query["year"], out int year))
            {
                return new BadRequestObjectResult(new { error = "Year parameter is required" });
            }

            // Get the uploaded file
            var formCollection = await req.ReadFormAsync();
            var file = formCollection.Files.GetFile("file");

            if (file == null || file.Length == 0)
            {
                return new BadRequestObjectResult(new { error = "No file uploaded" });
            }

            _logger.LogInformation("Uploading week {Week} for year {Year}, file size: {Size} bytes",
                week, year, file.Length);

            // Read and validate the Excel file
            using var stream = file.OpenReadStream();
            var weeklyLines = await _excelService.ParseWeeklyLinesAsync(stream, week, year);

            if (weeklyLines.Games.Count == 0)
            {
                return new BadRequestObjectResult(new { error = "No games found in the uploaded file" });
            }

            // Upload to blob storage
            stream.Position = 0; // Reset stream
            await _storageService.UploadWeeklyLinesAsync(stream, week, year);

            _logger.LogInformation("Successfully uploaded {Count} games for week {Week}",
                weeklyLines.Games.Count, week);

            return new OkObjectResult(new
            {
                success = true,
                week = week,
                year = year,
                gamesCount = weeklyLines.Games.Count,
                message = $"Successfully uploaded {weeklyLines.Games.Count} games for Week {week}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading lines");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}
