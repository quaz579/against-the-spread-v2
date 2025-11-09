using AgainstTheSpread.Core.Models;
using AgainstTheSpread.Core.Services;
using FluentAssertions;
using OfficeOpenXml;

namespace AgainstTheSpread.Tests.Services;

public class ExcelServiceTests : IDisposable
{
    private readonly ExcelService _excelService;

    public ExcelServiceTests()
    {
        _excelService = new ExcelService();
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    [Fact]
    public async Task ParseWeeklyLinesAsync_WithValidFile_ParsesCorrectly()
    {
        // Arrange
        var testData = CreateWeek1LinesExcel();
        using var stream = new MemoryStream(testData);

        // Act
        var result = await _excelService.ParseWeeklyLinesAsync(stream);

        // Assert
        result.Should().NotBeNull();
        result.Week.Should().Be(1);
        result.Games.Should().NotBeEmpty();
        result.Games.Should().HaveCount(4); // Test file has 4 games
    }

    [Fact]
    public async Task ParseWeeklyLinesAsync_ParsesGameDetails_Correctly()
    {
        // Arrange
        var testData = CreateWeek1LinesExcel();
        using var stream = new MemoryStream(testData);

        // Act
        var result = await _excelService.ParseWeeklyLinesAsync(stream);

        // Assert - Check first game (Boise State @ South Florida)
        var firstGame = result.Games.FirstOrDefault(g => g.Favorite == "Boise State");
        firstGame.Should().NotBeNull();
        firstGame!.Line.Should().Be(-10m);
        firstGame.VsAt.Should().Be("at");
        firstGame.Underdog.Should().Be("South Florida");
    }

    [Fact]
    public async Task ParseWeeklyLinesAsync_WithEmptyFile_ThrowsFormatException()
    {
        // Arrange
        var emptyExcel = CreateEmptyExcel();
        using var stream = new MemoryStream(emptyExcel);

        // Act & Assert
        await Assert.ThrowsAsync<FormatException>(
            async () => await _excelService.ParseWeeklyLinesAsync(stream));
    }

    [Fact]
    public async Task ParseWeeklyLinesAsync_WithoutWeekNumber_ThrowsFormatException()
    {
        // Arrange
        var excelWithoutWeek = CreateExcelWithoutWeekNumber();
        using var stream = new MemoryStream(excelWithoutWeek);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FormatException>(
            async () => await _excelService.ParseWeeklyLinesAsync(stream));
        exception.Message.Should().Contain("Could not find week number");
    }

    [Fact]
    public async Task GeneratePicksExcelAsync_WithValidPicks_GeneratesCorrectFormat()
    {
        // Arrange
        var userPicks = new UserPicks
        {
            Name = "Gary Harris",
            Week = 1,
            Year = 2024,
            Picks = new List<string> { "Notre Dame", "Akron", "Michigan", "Alabama", "Clemson", "FSU" },
            SubmittedAt = DateTime.UtcNow
        };

        // Act
        var result = await _excelService.GeneratePicksExcelAsync(userPicks);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();

        // Verify the content
        using var package = new ExcelPackage(new MemoryStream(result));
        var worksheet = package.Workbook.Worksheets[0];
        
        // Row 1 and 2 should be empty
        worksheet.Cells[1, 1].Text.Should().BeEmpty();
        worksheet.Cells[2, 1].Text.Should().BeEmpty();
        
        // Row 3 should have headers
        worksheet.Cells[3, 1].Text.Should().Be("Name");
        worksheet.Cells[3, 2].Text.Should().Be("Pick 1");
        worksheet.Cells[3, 7].Text.Should().Be("Pick 6");
        
        // Row 4 should have user data
        worksheet.Cells[4, 1].Text.Should().Be("Gary Harris");
        worksheet.Cells[4, 2].Text.Should().Be("Notre Dame");
        worksheet.Cells[4, 7].Text.Should().Be("FSU");
    }

    [Fact]
    public async Task GeneratePicksExcelAsync_WithInvalidPicks_ThrowsArgumentException()
    {
        // Arrange
        var invalidPicks = new UserPicks
        {
            Name = "Test User",
            Week = 1,
            Year = 2024,
            Picks = new List<string> { "Team1", "Team2" } // Only 2 picks, needs 6
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _excelService.GeneratePicksExcelAsync(invalidPicks));
    }

    [Fact]
    public async Task GeneratePicksExcelAsync_CreatesExcelWithPicksSheet()
    {
        // Arrange
        var userPicks = new UserPicks
        {
            Name = "Test User",
            Week = 1,
            Year = 2024,
            Picks = new List<string> { "Team1", "Team2", "Team3", "Team4", "Team5", "Team6" },
            SubmittedAt = DateTime.UtcNow
        };

        // Act
        var result = await _excelService.GeneratePicksExcelAsync(userPicks);

        // Assert
        using var package = new ExcelPackage(new MemoryStream(result));
        package.Workbook.Worksheets.Should().HaveCount(1);
        package.Workbook.Worksheets[0].Name.Should().Be("Picks");
    }

    [Theory]
    [InlineData("Alabama", -9.5)]
    [InlineData("Ohio State", -3.5)]
    [InlineData("Clemson", -2.5)]
    public async Task ParseWeeklyLinesAsync_ParsesSpecificGames_Correctly(string favorite, double expectedLine)
    {
        // Arrange
        var testData = CreateWeek1LinesExcel();
        using var stream = new MemoryStream(testData);

        // Act
        var result = await _excelService.ParseWeeklyLinesAsync(stream);

        // Assert
        var game = result.Games.FirstOrDefault(g => g.Favorite == favorite);
        game.Should().NotBeNull();
        game!.Line.Should().Be((decimal)expectedLine);
    }

    // Helper methods to create test Excel files

    private byte[] CreateWeek1LinesExcel()
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Lines");

        // Empty rows
        worksheet.Cells[1, 1].Value = string.Empty;
        worksheet.Cells[2, 1].Value = string.Empty;

        // Week header
        worksheet.Cells[3, 1].Value = "WEEK 1";
        worksheet.Cells[4, 1].Value = string.Empty;

        // Column headers
        worksheet.Cells[5, 2].Value = "Favorite";
        worksheet.Cells[5, 3].Value = "Line";
        worksheet.Cells[5, 4].Value = "vs/at";
        worksheet.Cells[5, 5].Value = "Under Dog";
        worksheet.Cells[6, 1].Value = string.Empty;

        // Date header
        worksheet.Cells[7, 1].Value = "Thursday, August 28, 2025";
        worksheet.Cells[8, 1].Value = string.Empty;

        // Games
        int row = 9;
        worksheet.Cells[row, 2].Value = "Boise State";
        worksheet.Cells[row, 3].Value = -10;
        worksheet.Cells[row, 4].Value = "at";
        worksheet.Cells[row, 5].Value = "South Florida";

        row++;
        worksheet.Cells[row, 2].Value = "Alabama";
        worksheet.Cells[row, 3].Value = -9.5;
        worksheet.Cells[row, 4].Value = "vs";
        worksheet.Cells[row, 5].Value = "Florida State";

        row++;
        worksheet.Cells[row, 2].Value = "Ohio State";
        worksheet.Cells[row, 3].Value = -3.5;
        worksheet.Cells[row, 4].Value = "vs";
        worksheet.Cells[row, 5].Value = "Texas";

        row++;
        worksheet.Cells[row, 2].Value = "Clemson";
        worksheet.Cells[row, 3].Value = -2.5;
        worksheet.Cells[row, 4].Value = "at";
        worksheet.Cells[row, 5].Value = "LSU";

        return package.GetAsByteArray();
    }

    private byte[] CreateEmptyExcel()
    {
        using var package = new ExcelPackage();
        package.Workbook.Worksheets.Add("Sheet1");
        return package.GetAsByteArray();
    }

    private byte[] CreateExcelWithoutWeekNumber()
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Lines");

        // Headers without week
        worksheet.Cells[1, 2].Value = "Favorite";
        worksheet.Cells[1, 3].Value = "Line";
        worksheet.Cells[1, 4].Value = "vs/at";
        worksheet.Cells[1, 5].Value = "Under Dog";

        // A game
        worksheet.Cells[2, 2].Value = "Team A";
        worksheet.Cells[2, 3].Value = -7;
        worksheet.Cells[2, 4].Value = "vs";
        worksheet.Cells[2, 5].Value = "Team B";

        return package.GetAsByteArray();
    }
}
