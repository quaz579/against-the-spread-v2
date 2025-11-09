using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Core.Models;
using OfficeOpenXml;

namespace AgainstTheSpread.Core.Services;

/// <summary>
/// Service for parsing and generating Excel files for weekly lines and user picks
/// </summary>
public class ExcelService : IExcelService
{
    public ExcelService()
    {
        // Set EPPlus license context (non-commercial use)
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    /// <summary>
    /// Parses the weekly lines Excel file uploaded by admin
    /// Format: Empty rows at top, then "WEEK N", then blank row, then headers, then games grouped by date
    /// </summary>
    public async Task<WeeklyLines> ParseWeeklyLinesAsync(Stream excelStream, CancellationToken cancellationToken = default)
    {
        using var package = new ExcelPackage(excelStream);
        var worksheet = package.Workbook.Worksheets[0];

        var weeklyLines = new WeeklyLines
        {
            Games = new List<Game>(),
            UploadedAt = DateTime.UtcNow,
            Year = DateTime.UtcNow.Year
        };

        // Find the week number (format: "WEEK N")
        for (int row = 1; row <= 10; row++)
        {
            var cellValue = worksheet.Cells[row, 1].Text?.Trim();
            if (!string.IsNullOrEmpty(cellValue) && cellValue.StartsWith("WEEK ", StringComparison.OrdinalIgnoreCase))
            {
                var weekPart = cellValue.Replace("WEEK ", "", StringComparison.OrdinalIgnoreCase).Trim();
                if (int.TryParse(weekPart, out int week))
                {
                    weeklyLines.Week = week;
                    break;
                }
            }
        }

        if (weeklyLines.Week == 0)
        {
            throw new FormatException("Could not find week number in Excel file. Expected 'WEEK N' format.");
        }

        // Find the header row (contains "Favorite", "Line", "vs/at", "Under Dog")
        int headerRow = 0;
        for (int row = 1; row <= 20; row++)
        {
            var col2Value = worksheet.Cells[row, 2].Text?.Trim();
            if (col2Value?.Equals("Favorite", StringComparison.OrdinalIgnoreCase) == true)
            {
                headerRow = row;
                break;
            }
        }

        if (headerRow == 0)
        {
            throw new FormatException("Could not find header row with 'Favorite' column.");
        }

        // Parse games starting after header row
        DateTime? currentGameDate = null;
        for (int row = headerRow + 1; row <= worksheet.Dimension.End.Row; row++)
        {
            var col1Value = worksheet.Cells[row, 1].Text?.Trim();
            var col2Value = worksheet.Cells[row, 2].Text?.Trim();

            // Check if this is a date header row
            if (!string.IsNullOrEmpty(col1Value) && string.IsNullOrEmpty(col2Value))
            {
                // Try to parse as date
                if (DateTime.TryParse(col1Value, out DateTime parsedDate))
                {
                    currentGameDate = parsedDate;
                    continue;
                }
            }

            // Check if this is a game row (has Favorite in column 2)
            if (string.IsNullOrEmpty(col2Value))
                continue;

            var lineText = worksheet.Cells[row, 3].Text?.Trim();
            var vsAt = worksheet.Cells[row, 4].Text?.Trim();
            var underdog = worksheet.Cells[row, 5].Text?.Trim();

            // Skip rows without complete game data
            if (string.IsNullOrEmpty(lineText) || string.IsNullOrEmpty(underdog))
                continue;

            // Parse the line (e.g., "-7.5")
            if (!decimal.TryParse(lineText, out decimal line))
                continue;

            var game = new Game
            {
                Favorite = col2Value,
                Line = line,
                VsAt = vsAt ?? string.Empty,
                Underdog = underdog,
                GameDate = currentGameDate ?? DateTime.UtcNow
            };

            weeklyLines.Games.Add(game);
        }

        if (weeklyLines.Games.Count == 0)
        {
            throw new FormatException("No games found in Excel file.");
        }

        return await Task.FromResult(weeklyLines);
    }

    /// <summary>
    /// Generates an Excel file with user picks in the EXACT format of "Weekly Picks Example.csv"
    /// Format: 2 empty rows, then header row (Name, Pick 1-6), then user picks row
    /// </summary>
    public async Task<byte[]> GeneratePicksExcelAsync(UserPicks userPicks, CancellationToken cancellationToken = default)
    {
        if (!userPicks.IsValid())
        {
            throw new ArgumentException($"Invalid user picks: {userPicks.GetValidationError()}", nameof(userPicks));
        }

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Picks");

        // Row 1 and 2: Empty rows (with empty columns A-G to match format)
        for (int col = 1; col <= 7; col++)
        {
            worksheet.Cells[1, col].Value = string.Empty;
            worksheet.Cells[2, col].Value = string.Empty;
        }

        // Row 3: Headers
        worksheet.Cells[3, 1].Value = "Name";
        worksheet.Cells[3, 2].Value = "Pick 1";
        worksheet.Cells[3, 3].Value = "Pick 2";
        worksheet.Cells[3, 4].Value = "Pick 3";
        worksheet.Cells[3, 5].Value = "Pick 4";
        worksheet.Cells[3, 6].Value = "Pick 5";
        worksheet.Cells[3, 7].Value = "Pick 6";

        // Row 4: User picks
        worksheet.Cells[4, 1].Value = userPicks.Name;
        for (int i = 0; i < userPicks.Picks.Count; i++)
        {
            worksheet.Cells[4, i + 2].Value = userPicks.Picks[i];
        }

        // Auto-fit columns for readability
        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

        return await Task.FromResult(package.GetAsByteArray());
    }
}
