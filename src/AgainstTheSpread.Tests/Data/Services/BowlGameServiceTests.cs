using AgainstTheSpread.Data;
using AgainstTheSpread.Data.Entities;
using AgainstTheSpread.Data.Interfaces;
using AgainstTheSpread.Data.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace AgainstTheSpread.Tests.Data.Services;

/// <summary>
/// Tests for BowlGameService implementation.
/// Uses InMemory database provider for testing.
/// </summary>
public class BowlGameServiceTests : IDisposable
{
    private readonly AtsDbContext _context;
    private readonly IBowlGameService _bowlGameService;
    private readonly Mock<ILogger<BowlGameService>> _mockLogger;
    private readonly Guid _testAdminId;

    public BowlGameServiceTests()
    {
        var options = new DbContextOptionsBuilder<AtsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AtsDbContext(options);
        _mockLogger = new Mock<ILogger<BowlGameService>>();
        _bowlGameService = new BowlGameService(_context, _mockLogger.Object);
        _testAdminId = Guid.NewGuid();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region Helper Methods

    private BowlGameEntity CreateBowlGame(int year, int gameNumber, string bowlName, string favorite, string underdog, decimal line, DateTime gameDate)
    {
        var game = new BowlGameEntity
        {
            Year = year,
            GameNumber = gameNumber,
            BowlName = bowlName,
            Favorite = favorite,
            Underdog = underdog,
            Line = line,
            GameDate = gameDate
        };
        _context.BowlGames.Add(game);
        _context.SaveChanges();
        return game;
    }

    #endregion

    #region SyncBowlGamesAsync Tests

    [Fact]
    public async Task SyncBowlGamesAsync_WithNewGames_CreatesGames()
    {
        // Arrange
        var games = new List<BowlGameSyncInput>
        {
            new(1, "Rose Bowl", "USC", "Penn State", -3.5m, new DateTime(2025, 1, 1)),
            new(2, "Sugar Bowl", "Georgia", "Notre Dame", -7m, new DateTime(2025, 1, 1))
        };

        // Act
        var result = await _bowlGameService.SyncBowlGamesAsync(2024, games);

        // Assert
        result.Should().Be(2);
        var savedGames = await _context.BowlGames.Where(g => g.Year == 2024).ToListAsync();
        savedGames.Should().HaveCount(2);
        savedGames.Should().Contain(g => g.BowlName == "Rose Bowl");
        savedGames.Should().Contain(g => g.BowlName == "Sugar Bowl");
    }

    [Fact]
    public async Task SyncBowlGamesAsync_WithExistingGame_UpdatesGame()
    {
        // Arrange
        CreateBowlGame(2024, 1, "Rose Bowl", "USC", "Penn State", -3.5m, new DateTime(2025, 1, 1));

        var updates = new List<BowlGameSyncInput>
        {
            new(1, "Rose Bowl", "USC", "Penn State", -5.5m, new DateTime(2025, 1, 1)) // Updated line
        };

        // Act
        var result = await _bowlGameService.SyncBowlGamesAsync(2024, updates);

        // Assert
        result.Should().Be(1);
        var game = await _context.BowlGames.FirstOrDefaultAsync(g => g.Year == 2024 && g.GameNumber == 1);
        game.Should().NotBeNull();
        game!.Line.Should().Be(-5.5m);
    }

    [Fact]
    public async Task SyncBowlGamesAsync_WithEmptyList_ReturnsZero()
    {
        // Arrange
        var games = new List<BowlGameSyncInput>();

        // Act
        var result = await _bowlGameService.SyncBowlGamesAsync(2024, games);

        // Assert
        result.Should().Be(0);
    }

    #endregion

    #region GetBowlGamesAsync Tests

    [Fact]
    public async Task GetBowlGamesAsync_ReturnsGamesForYear()
    {
        // Arrange
        CreateBowlGame(2024, 1, "Rose Bowl", "USC", "Penn State", -3.5m, new DateTime(2025, 1, 1));
        CreateBowlGame(2024, 2, "Sugar Bowl", "Georgia", "Notre Dame", -7m, new DateTime(2025, 1, 1));
        CreateBowlGame(2023, 1, "Old Rose Bowl", "Michigan", "Alabama", -2m, new DateTime(2024, 1, 1)); // Different year

        // Act
        var result = await _bowlGameService.GetBowlGamesAsync(2024);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(g => g.Year == 2024);
    }

    [Fact]
    public async Task GetBowlGamesAsync_ReturnsOrderedByGameNumber()
    {
        // Arrange
        CreateBowlGame(2024, 3, "Peach Bowl", "Team A", "Team B", -5m, new DateTime(2025, 1, 2));
        CreateBowlGame(2024, 1, "Rose Bowl", "Team C", "Team D", -3m, new DateTime(2025, 1, 1));
        CreateBowlGame(2024, 2, "Sugar Bowl", "Team E", "Team F", -7m, new DateTime(2025, 1, 1));

        // Act
        var result = await _bowlGameService.GetBowlGamesAsync(2024);

        // Assert
        result.Should().HaveCount(3);
        result[0].GameNumber.Should().Be(1);
        result[1].GameNumber.Should().Be(2);
        result[2].GameNumber.Should().Be(3);
    }

    [Fact]
    public async Task GetBowlGamesAsync_WithNoGamesForYear_ReturnsEmptyList()
    {
        // Arrange - no games

        // Act
        var result = await _bowlGameService.GetBowlGamesAsync(2024);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingGame_ReturnsGame()
    {
        // Arrange
        var game = CreateBowlGame(2024, 1, "Rose Bowl", "USC", "Penn State", -3.5m, new DateTime(2025, 1, 1));

        // Act
        var result = await _bowlGameService.GetByIdAsync(game.Id);

        // Assert
        result.Should().NotBeNull();
        result!.BowlName.Should().Be("Rose Bowl");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentGame_ReturnsNull()
    {
        // Arrange - no game

        // Act
        var result = await _bowlGameService.GetByIdAsync(9999);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region IsGameLockedAsync Tests

    [Fact]
    public async Task IsGameLockedAsync_WithPastGameDate_ReturnsTrue()
    {
        // Arrange
        var game = CreateBowlGame(2024, 1, "Rose Bowl", "USC", "Penn State", -3.5m, DateTime.UtcNow.AddHours(-1));

        // Act
        var result = await _bowlGameService.IsGameLockedAsync(game.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsGameLockedAsync_WithFutureGameDate_ReturnsFalse()
    {
        // Arrange
        var game = CreateBowlGame(2024, 1, "Rose Bowl", "USC", "Penn State", -3.5m, DateTime.UtcNow.AddDays(1));

        // Act
        var result = await _bowlGameService.IsGameLockedAsync(game.Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsGameLockedAsync_WithNonExistentGame_ReturnsNull()
    {
        // Arrange - no game

        // Act
        var result = await _bowlGameService.IsGameLockedAsync(9999);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetTotalGamesCountAsync Tests

    [Fact]
    public async Task GetTotalGamesCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        CreateBowlGame(2024, 1, "Rose Bowl", "USC", "Penn State", -3.5m, new DateTime(2025, 1, 1));
        CreateBowlGame(2024, 2, "Sugar Bowl", "Georgia", "Notre Dame", -7m, new DateTime(2025, 1, 1));
        CreateBowlGame(2024, 3, "Peach Bowl", "Team A", "Team B", -5m, new DateTime(2025, 1, 2));

        // Act
        var result = await _bowlGameService.GetTotalGamesCountAsync(2024);

        // Assert
        result.Should().Be(3);
    }

    [Fact]
    public async Task GetTotalGamesCountAsync_OnlyCountsSpecifiedYear()
    {
        // Arrange
        CreateBowlGame(2024, 1, "Rose Bowl", "USC", "Penn State", -3.5m, new DateTime(2025, 1, 1));
        CreateBowlGame(2023, 1, "Old Rose Bowl", "Michigan", "Alabama", -2m, new DateTime(2024, 1, 1));

        // Act
        var result = await _bowlGameService.GetTotalGamesCountAsync(2024);

        // Assert
        result.Should().Be(1);
    }

    #endregion

    #region EnterResultAsync Tests

    [Fact]
    public async Task EnterResultAsync_WithFavoriteWinningAndCoveringSpread_SetsFavoriteAsSpreadWinner()
    {
        // Arrange - Favorite favored by 7, wins by 10
        var game = CreateBowlGame(2024, 1, "Rose Bowl", "USC", "Penn State", -7m, new DateTime(2025, 1, 1));

        // Act
        var result = await _bowlGameService.EnterResultAsync(game.Id, 31, 21, _testAdminId);

        // Assert
        result.Should().NotBeNull();
        result!.FavoriteScore.Should().Be(31);
        result.UnderdogScore.Should().Be(21);
        result.SpreadWinner.Should().Be("USC");
        result.OutrightWinner.Should().Be("USC");
        result.IsPush.Should().BeFalse();
    }

    [Fact]
    public async Task EnterResultAsync_WithFavoriteWinningButNotCoveringSpread_SetsUnderdogAsSpreadWinner()
    {
        // Arrange - Favorite favored by 7, wins by only 3
        var game = CreateBowlGame(2024, 1, "Rose Bowl", "USC", "Penn State", -7m, new DateTime(2025, 1, 1));

        // Act
        var result = await _bowlGameService.EnterResultAsync(game.Id, 24, 21, _testAdminId);

        // Assert
        result.Should().NotBeNull();
        result!.SpreadWinner.Should().Be("Penn State");
        result.OutrightWinner.Should().Be("USC");
        result.IsPush.Should().BeFalse();
    }

    [Fact]
    public async Task EnterResultAsync_WithUnderdogWinning_SetsUnderdogAsWinner()
    {
        // Arrange
        var game = CreateBowlGame(2024, 1, "Rose Bowl", "USC", "Penn State", -7m, new DateTime(2025, 1, 1));

        // Act
        var result = await _bowlGameService.EnterResultAsync(game.Id, 14, 21, _testAdminId);

        // Assert
        result.Should().NotBeNull();
        result!.SpreadWinner.Should().Be("Penn State");
        result.OutrightWinner.Should().Be("Penn State");
    }

    [Fact]
    public async Task EnterResultAsync_WithExactSpread_SetsPush()
    {
        // Arrange - Favorite favored by 7, wins by exactly 7
        var game = CreateBowlGame(2024, 1, "Rose Bowl", "USC", "Penn State", -7m, new DateTime(2025, 1, 1));

        // Act
        var result = await _bowlGameService.EnterResultAsync(game.Id, 28, 21, _testAdminId);

        // Assert
        result.Should().NotBeNull();
        result!.IsPush.Should().BeTrue();
        result.SpreadWinner.Should().BeNull();
        result.OutrightWinner.Should().Be("USC");
    }

    [Fact]
    public async Task EnterResultAsync_WithNonExistentGame_ReturnsNull()
    {
        // Arrange - no game

        // Act
        var result = await _bowlGameService.EnterResultAsync(9999, 21, 14, _testAdminId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task EnterResultAsync_SetsEntryMetadata()
    {
        // Arrange
        var game = CreateBowlGame(2024, 1, "Rose Bowl", "USC", "Penn State", -7m, new DateTime(2025, 1, 1));

        // Act
        var result = await _bowlGameService.EnterResultAsync(game.Id, 31, 21, _testAdminId);

        // Assert
        result.Should().NotBeNull();
        result!.ResultEnteredAt.Should().NotBeNull();
        result.ResultEnteredBy.Should().Be(_testAdminId);
    }

    #endregion

    #region BulkEnterResultsAsync Tests

    [Fact]
    public async Task BulkEnterResultsAsync_WithMultipleResults_EntersAll()
    {
        // Arrange
        var game1 = CreateBowlGame(2024, 1, "Rose Bowl", "USC", "Penn State", -7m, new DateTime(2025, 1, 1));
        var game2 = CreateBowlGame(2024, 2, "Sugar Bowl", "Georgia", "Notre Dame", -3m, new DateTime(2025, 1, 1));

        var results = new List<BowlGameResultInput>
        {
            new(game1.Id, 31, 21),
            new(game2.Id, 28, 14)
        };

        // Act
        var result = await _bowlGameService.BulkEnterResultsAsync(results, _testAdminId);

        // Assert
        result.Success.Should().BeTrue();
        result.ResultsEntered.Should().Be(2);
        result.ResultsFailed.Should().Be(0);
    }

    [Fact]
    public async Task BulkEnterResultsAsync_WithNonExistentGame_FailsThatGame()
    {
        // Arrange
        var game1 = CreateBowlGame(2024, 1, "Rose Bowl", "USC", "Penn State", -7m, new DateTime(2025, 1, 1));

        var results = new List<BowlGameResultInput>
        {
            new(game1.Id, 31, 21),
            new(9999, 28, 14) // Non-existent
        };

        // Act
        var result = await _bowlGameService.BulkEnterResultsAsync(results, _testAdminId);

        // Assert
        result.Success.Should().BeTrue();
        result.ResultsEntered.Should().Be(1);
        result.ResultsFailed.Should().Be(1);
        result.FailedResults.Should().Contain(f => f.BowlGameId == 9999);
    }

    [Fact]
    public async Task BulkEnterResultsAsync_WithEmptyList_ReturnsSuccess()
    {
        // Arrange
        var results = new List<BowlGameResultInput>();

        // Act
        var result = await _bowlGameService.BulkEnterResultsAsync(results, _testAdminId);

        // Assert
        result.Success.Should().BeTrue();
        result.ResultsEntered.Should().Be(0);
    }

    #endregion
}
