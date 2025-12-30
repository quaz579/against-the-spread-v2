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
    /// Format: Empty rows at top, then "WEEK N" (optional), then blank row, then headers, then games grouped by date
    /// </summary>
    public async Task<WeeklyLines> ParseWeeklyLinesAsync(Stream excelStream, int? week = null, int? year = null, CancellationToken cancellationToken = default)
    {
        using var package = new ExcelPackage(excelStream);
        var worksheet = package.Workbook.Worksheets[0];

        var weeklyLines = new WeeklyLines
        {
            Games = new List<Game>(),
            UploadedAt = DateTime.UtcNow,
            Week = week ?? 0,
            Year = year ?? DateTime.UtcNow.Year
        };

        // Try to find the week number from file if not provided (format: "WEEK N")
        if (!week.HasValue)
        {
            for (int row = 1; row <= 10; row++)
            {
                // Search across columns for "WEEK N" format
                for (int col = 1; col <= 10; col++)
                {
                    var cellValue = worksheet.Cells[row, col].Text?.Trim();
                    if (!string.IsNullOrEmpty(cellValue) && cellValue.StartsWith("WEEK ", StringComparison.OrdinalIgnoreCase))
                    {
                        var weekPart = cellValue.Replace("WEEK ", "", StringComparison.OrdinalIgnoreCase).Trim();
                        if (int.TryParse(weekPart, out int fileWeek))
                        {
                            weeklyLines.Week = fileWeek;
                            break;
                        }
                    }
                }
                if (weeklyLines.Week > 0) break;
            }

            if (weeklyLines.Week == 0)
            {
                throw new FormatException("Could not find week number in Excel file. Expected 'WEEK N' format or week parameter.");
            }
        }

        // Find the header row dynamically by searching for "Favorite" in any column
        int headerRow = 0;
        int favoriteCol = 0;
        int lineCol = 0;
        int vsAtCol = 0;
        int underdogCol = 0;

        // Search more columns to handle different indentation levels (Week 1, Week 11, etc.)
        int maxSearchCol = Math.Min(worksheet.Dimension?.End.Column ?? 20, 20);

        for (int row = 1; row <= 20; row++)
        {
            // Search across columns for the header
            for (int col = 1; col <= maxSearchCol; col++)
            {
                var cellValue = worksheet.Cells[row, col].Text?.Trim();
                if (cellValue?.Equals("Favorite", StringComparison.OrdinalIgnoreCase) == true)
                {
                    headerRow = row;
                    favoriteCol = col;

                    // Find the other columns relative to Favorite (search up to 10 columns ahead)
                    int maxRelativeSearch = Math.Min(col + 10, worksheet.Dimension?.End.Column ?? col + 10);
                    for (int searchCol = col; searchCol <= maxRelativeSearch; searchCol++)
                    {
                        var headerText = worksheet.Cells[row, searchCol].Text?.Trim();
                        if (headerText?.Equals("Line", StringComparison.OrdinalIgnoreCase) == true)
                            lineCol = searchCol;
                        else if (headerText?.Contains("vs/at", StringComparison.OrdinalIgnoreCase) == true)
                            vsAtCol = searchCol;
                        else if (headerText?.Contains("Under Dog", StringComparison.OrdinalIgnoreCase) == true ||
                                headerText?.Equals("Underdog", StringComparison.OrdinalIgnoreCase) == true)
                            underdogCol = searchCol;
                    }
                    break;
                }
            }
            if (headerRow > 0) break;
        }

        if (headerRow == 0 || favoriteCol == 0 || lineCol == 0 || underdogCol == 0)
        {
            var missing = new List<string>();
            if (headerRow == 0) missing.Add("header row");
            if (favoriteCol == 0) missing.Add("Favorite");
            if (lineCol == 0) missing.Add("Line");
            if (underdogCol == 0) missing.Add("Under Dog");
            throw new FormatException($"Could not find required columns: {string.Join(", ", missing)}. Found header at row {headerRow}, Favorite at col {favoriteCol}, Line at col {lineCol}, Under Dog at col {underdogCol}");
        }

        // Find the date column (usually before Favorite column)
        int dateCol = 0;
        for (int col = 1; col < favoriteCol; col++)
        {
            var cellValue = worksheet.Cells[headerRow + 1, col].Text?.Trim();
            if (!string.IsNullOrEmpty(cellValue) && DateTime.TryParse(cellValue, out _))
            {
                dateCol = col;
                break;
            }
        }

        // If no date found before favorite, check the same column as favorite for dates
        if (dateCol == 0)
        {
            var cellValue = worksheet.Cells[headerRow + 1, favoriteCol].Text?.Trim();
            if (!string.IsNullOrEmpty(cellValue) && DateTime.TryParse(cellValue, out _))
            {
                dateCol = favoriteCol;
            }
        }

        // Parse games starting after header row
        DateTime? currentGameDate = null;
        for (int row = headerRow + 1; row <= worksheet.Dimension.End.Row; row++)
        {
            var favoriteValue = worksheet.Cells[row, favoriteCol].Text?.Trim();

            // Check if this is a date header row
            if (dateCol > 0)
            {
                var dateValue = worksheet.Cells[row, dateCol].Text?.Trim();
                if (!string.IsNullOrEmpty(dateValue) && DateTime.TryParse(dateValue, out DateTime parsedDate))
                {
                    currentGameDate = parsedDate;
                    continue;
                }
            }

            // Try to find date in any non-empty cell in columns before favorite
            if (string.IsNullOrEmpty(favoriteValue))
            {
                for (int col = 1; col < favoriteCol; col++)
                {
                    var cellValue = worksheet.Cells[row, col].Text?.Trim();
                    if (!string.IsNullOrEmpty(cellValue) && DateTime.TryParse(cellValue, out DateTime parsedDate))
                    {
                        currentGameDate = parsedDate;
                        break;
                    }
                }
                continue;
            }

            var lineText = worksheet.Cells[row, lineCol].Text?.Trim();
            var vsAt = vsAtCol > 0 ? worksheet.Cells[row, vsAtCol].Text?.Trim() : "vs";
            var underdog = worksheet.Cells[row, underdogCol].Text?.Trim();

            // Normalize vs/at values (handle typos like "a" instead of "at")
            if (!string.IsNullOrEmpty(vsAt))
            {
                vsAt = vsAt.ToLowerInvariant();
                if (vsAt == "a") vsAt = "at";
                else if (vsAt == "v") vsAt = "vs";
            }
            else
            {
                vsAt = "vs";
            }

            // Skip rows without complete game data
            if (string.IsNullOrEmpty(lineText) || string.IsNullOrEmpty(underdog))
                continue;

            // Parse the line (e.g., "-7.5")
            if (!decimal.TryParse(lineText, out decimal line))
                continue;

            // Calculate the game date, adjusting year if provided and different from parsed date
            var gameDate = currentGameDate ?? DateTime.UtcNow;
            if (year.HasValue && gameDate.Year != year.Value)
            {
                // Adjust the year to match the provided year parameter
                // This handles cases where Excel file has dates from previous season
                gameDate = new DateTime(year.Value, gameDate.Month, gameDate.Day,
                    gameDate.Hour, gameDate.Minute, gameDate.Second, gameDate.Kind);
            }

            var game = new Game
            {
                Favorite = favoriteValue,
                Line = line,
                VsAt = vsAt ?? "vs",
                Underdog = underdog,
                GameDate = gameDate
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
