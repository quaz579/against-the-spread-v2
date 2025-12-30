using AgainstTheSpread.Data;
using AgainstTheSpread.Data.Entities;
using AgainstTheSpread.Data.Interfaces;
using AgainstTheSpread.Data.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AgainstTheSpread.Tests.Data.Services;

public class ResultServiceTests
{
    private readonly AtsDbContext _context;
    private readonly ResultService _service;
    private readonly Guid _adminUserId = Guid.NewGuid();

    public ResultServiceTests()
    {
        var options = new DbContextOptionsBuilder<AtsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AtsDbContext(options);
        var logger = Mock.Of<ILogger<ResultService>>();
        _service = new ResultService(_context, logger);
    }

    #region CalculateSpreadWinner Tests

    [Fact]
    public void CalculateSpreadWinner_FavoriteCovers_ReturnsFavorite()
    {
        // Favorite is -7, wins by 10
        var (winner, isPush) = _service.CalculateSpreadWinner("Alabama", "Tennessee", -7m, 24, 14);

        winner.Should().Be("Alabama");
        isPush.Should().BeFalse();
    }

    [Fact]
    public void CalculateSpreadWinner_UnderdogCovers_ReturnsUnderdog()
    {
        // Favorite is -7, wins by 3 (underdog covers)
        var (winner, isPush) = _service.CalculateSpreadWinner("Alabama", "Tennessee", -7m, 24, 21);

        winner.Should().Be("Tennessee");
        isPush.Should().BeFalse();
    }

    [Fact]
    public void CalculateSpreadWinner_ExactSpread_ReturnsPush()
    {
        // Favorite is -7, wins by exactly 7
        var (winner, isPush) = _service.CalculateSpreadWinner("Alabama", "Tennessee", -7m, 24, 17);

        winner.Should().BeNull();
        isPush.Should().BeTrue();
    }

    [Fact]
    public void CalculateSpreadWinner_UnderdogWinsOutright_ReturnsUnderdog()
    {
        // Underdog wins outright
        var (winner, isPush) = _service.CalculateSpreadWinner("Alabama", "Tennessee", -7m, 14, 21);

        winner.Should().Be("Tennessee");
        isPush.Should().BeFalse();
    }

    [Fact]
    public void CalculateSpreadWinner_HalfPointSpread_CannotPush()
    {
        // With -7.5 spread, can't push
        var (winner, isPush) = _service.CalculateSpreadWinner("Alabama", "Tennessee", -7.5m, 24, 17);

        winner.Should().Be("Tennessee"); // Favorite won by 7, but needed 7.5
        isPush.Should().BeFalse();
    }

    [Fact]
    public void CalculateSpreadWinner_ZeroSpread_TieIsPush()
    {
        // Pick 'em game (0 spread), tie
        var (winner, isPush) = _service.CalculateSpreadWinner("Alabama", "Tennessee", 0m, 21, 21);

        winner.Should().BeNull();
        isPush.Should().BeTrue();
    }

    #endregion

    #region EnterResultAsync Tests

    [Fact]
    public async Task EnterResultAsync_WithValidGame_EntersResult()
    {
        // Arrange
        var game = new GameEntity
        {
            Year = 2025,
            Week = 1,
            Favorite = "Alabama",
            Underdog = "Tennessee",
            Line = -7m,
            GameDate = DateTime.UtcNow.AddDays(-1)
        };
        _context.Games.Add(game);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.EnterResultAsync(game.Id, 24, 14, _adminUserId);

        // Assert
        result.Should().NotBeNull();
        result!.FavoriteScore.Should().Be(24);
        result.UnderdogScore.Should().Be(14);
        result.SpreadWinner.Should().Be("Alabama");
        result.IsPush.Should().BeFalse();
        result.ResultEnteredBy.Should().Be(_adminUserId);
        result.ResultEnteredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task EnterResultAsync_WithNonExistentGame_ReturnsNull()
    {
        // Act
        var result = await _service.EnterResultAsync(999, 24, 14, _adminUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task EnterResultAsync_OverwritesExistingResult()
    {
        // Arrange
        var game = new GameEntity
        {
            Year = 2025,
            Week = 1,
            Favorite = "Alabama",
            Underdog = "Tennessee",
            Line = -7m,
            GameDate = DateTime.UtcNow.AddDays(-1),
            FavoriteScore = 10,
            UnderdogScore = 10,
            SpreadWinner = null,
            IsPush = true
        };
        _context.Games.Add(game);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.EnterResultAsync(game.Id, 28, 14, _adminUserId);

        // Assert
        result.Should().NotBeNull();
        result!.FavoriteScore.Should().Be(28);
        result.UnderdogScore.Should().Be(14);
        result.SpreadWinner.Should().Be("Alabama");
        result.IsPush.Should().BeFalse();
    }

    #endregion

    #region BulkEnterResultsAsync Tests

    [Fact]
    public async Task BulkEnterResultsAsync_WithEmptyList_ReturnsSuccess()
    {
        // Act
        var result = await _service.BulkEnterResultsAsync(2025, 1, [], _adminUserId);

        // Assert
        result.Success.Should().BeTrue();
        result.ResultsEntered.Should().Be(0);
    }

    [Fact]
    public async Task BulkEnterResultsAsync_WithValidGames_EntersAllResults()
    {
        // Arrange
        var game1 = new GameEntity { Year = 2025, Week = 1, Favorite = "Alabama", Underdog = "Tennessee", Line = -7m, GameDate = DateTime.UtcNow.AddDays(-1) };
        var game2 = new GameEntity { Year = 2025, Week = 1, Favorite = "Georgia", Underdog = "Florida", Line = -10m, GameDate = DateTime.UtcNow.AddDays(-1) };
        _context.Games.AddRange(game1, game2);
        await _context.SaveChangesAsync();

        var results = new List<GameResultInput>
        {
            new(game1.Id, 24, 14),
            new(game2.Id, 31, 17)
        };

        // Act
        var result = await _service.BulkEnterResultsAsync(2025, 1, results, _adminUserId);

        // Assert
        result.Success.Should().BeTrue();
        result.ResultsEntered.Should().Be(2);
        result.ResultsFailed.Should().Be(0);

        var updatedGame1 = await _context.Games.FindAsync(game1.Id);
        updatedGame1!.SpreadWinner.Should().Be("Alabama");

        var updatedGame2 = await _context.Games.FindAsync(game2.Id);
        updatedGame2!.SpreadWinner.Should().Be("Georgia");
    }

    [Fact]
    public async Task BulkEnterResultsAsync_WithNonExistentGame_PartialSuccess()
    {
        // Arrange
        var game1 = new GameEntity { Year = 2025, Week = 1, Favorite = "Alabama", Underdog = "Tennessee", Line = -7m, GameDate = DateTime.UtcNow.AddDays(-1) };
        _context.Games.Add(game1);
        await _context.SaveChangesAsync();

        var results = new List<GameResultInput>
        {
            new(game1.Id, 24, 14),
            new(999, 31, 17) // Non-existent game
        };

        // Act
        var result = await _service.BulkEnterResultsAsync(2025, 1, results, _adminUserId);

        // Assert
        result.Success.Should().BeTrue();
        result.ResultsEntered.Should().Be(1);
        result.ResultsFailed.Should().Be(1);
        result.FailedResults.Should().ContainSingle(f => f.GameId == 999);
    }

    [Fact]
    public async Task BulkEnterResultsAsync_WithWrongWeekGame_RejectsGame()
    {
        // Arrange
        var game1 = new GameEntity { Year = 2025, Week = 2, Favorite = "Alabama", Underdog = "Tennessee", Line = -7m, GameDate = DateTime.UtcNow.AddDays(-1) };
        _context.Games.Add(game1);
        await _context.SaveChangesAsync();

        var results = new List<GameResultInput>
        {
            new(game1.Id, 24, 14)
        };

        // Act - Entering for week 1, but game is week 2
        var result = await _service.BulkEnterResultsAsync(2025, 1, results, _adminUserId);

        // Assert
        result.Success.Should().BeTrue();
        result.ResultsEntered.Should().Be(0);
        result.ResultsFailed.Should().Be(1);
    }

    [Fact]
    public async Task BulkEnterResultsAsync_WithNegativeScore_RejectsGame()
    {
        // Arrange
        var game1 = new GameEntity { Year = 2025, Week = 1, Favorite = "Alabama", Underdog = "Tennessee", Line = -7m, GameDate = DateTime.UtcNow.AddDays(-1) };
        _context.Games.Add(game1);
        await _context.SaveChangesAsync();

        var results = new List<GameResultInput>
        {
            new(game1.Id, -1, 14) // Negative score
        };

        // Act
        var result = await _service.BulkEnterResultsAsync(2025, 1, results, _adminUserId);

        // Assert
        result.Success.Should().BeTrue();
        result.ResultsEntered.Should().Be(0);
        result.ResultsFailed.Should().Be(1);
        result.FailedResults.Should().ContainSingle(f => f.Reason.Contains("negative"));
    }

    #endregion

    #region GetWeekResultsAsync Tests

    [Fact]
    public async Task GetWeekResultsAsync_ReturnsAllGamesForWeek()
    {
        // Arrange
        var game1 = new GameEntity { Year = 2025, Week = 1, Favorite = "Alabama", Underdog = "Tennessee", Line = -7m, GameDate = DateTime.UtcNow.AddDays(-1) };
        var game2 = new GameEntity { Year = 2025, Week = 1, Favorite = "Georgia", Underdog = "Florida", Line = -10m, GameDate = DateTime.UtcNow };
        var game3 = new GameEntity { Year = 2025, Week = 2, Favorite = "Ohio State", Underdog = "Michigan", Line = -3m, GameDate = DateTime.UtcNow.AddDays(1) };
        _context.Games.AddRange(game1, game2, game3);
        await _context.SaveChangesAsync();

        // Act
        var results = await _service.GetWeekResultsAsync(2025, 1);

        // Assert
        results.Should().HaveCount(2);
        results.Should().Contain(g => g.Favorite == "Alabama");
        results.Should().Contain(g => g.Favorite == "Georgia");
        results.Should().NotContain(g => g.Favorite == "Ohio State");
    }

    [Fact]
    public async Task GetWeekResultsAsync_OrdersByGameDate()
    {
        // Arrange
        var game1 = new GameEntity { Year = 2025, Week = 1, Favorite = "Alabama", Underdog = "Tennessee", Line = -7m, GameDate = DateTime.UtcNow.AddDays(1) };
        var game2 = new GameEntity { Year = 2025, Week = 1, Favorite = "Georgia", Underdog = "Florida", Line = -10m, GameDate = DateTime.UtcNow };
        _context.Games.AddRange(game1, game2);
        await _context.SaveChangesAsync();

        // Act
        var results = await _service.GetWeekResultsAsync(2025, 1);

        // Assert
        results[0].Favorite.Should().Be("Georgia");
        results[1].Favorite.Should().Be("Alabama");
    }

    [Fact]
    public async Task GetWeekResultsAsync_WithNoGames_ReturnsEmptyList()
    {
        // Act
        var results = await _service.GetWeekResultsAsync(2025, 1);

        // Assert
        results.Should().BeEmpty();
    }

    #endregion
}
