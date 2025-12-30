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
/// Tests for GameService implementation.
/// Uses InMemory database provider for testing.
/// </summary>
public class GameServiceTests : IDisposable
{
    private readonly AtsDbContext _context;
    private readonly IGameService _gameService;
    private readonly Mock<ILogger<GameService>> _mockLogger;

    public GameServiceTests()
    {
        var options = new DbContextOptionsBuilder<AtsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AtsDbContext(options);
        _mockLogger = new Mock<ILogger<GameService>>();
        _gameService = new GameService(_context, _mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region SyncGamesFromLinesAsync Tests

    [Fact]
    public async Task SyncGamesFromLinesAsync_WithNewGames_CreatesGames()
    {
        // Arrange
        var games = new List<GameSyncInput>
        {
            new("Alabama", "Auburn", -7.5m, new DateTime(2024, 9, 7, 19, 0, 0, DateTimeKind.Utc)),
            new("Georgia", "Florida", -10m, new DateTime(2024, 9, 7, 15, 30, 0, DateTimeKind.Utc))
        };

        // Act
        var result = await _gameService.SyncGamesFromLinesAsync(2024, 1, games);

        // Assert
        result.Should().Be(2);

        var gamesInDb = await _context.Games.Where(g => g.Year == 2024 && g.Week == 1).ToListAsync();
        gamesInDb.Should().HaveCount(2);
        gamesInDb.Should().Contain(g => g.Favorite == "Alabama" && g.Underdog == "Auburn");
        gamesInDb.Should().Contain(g => g.Favorite == "Georgia" && g.Underdog == "Florida");
    }

    [Fact]
    public async Task SyncGamesFromLinesAsync_WithExistingGames_UpdatesLine()
    {
        // Arrange - Pre-create a game
        var existingGame = new GameEntity
        {
            Year = 2024,
            Week = 1,
            Favorite = "Alabama",
            Underdog = "Auburn",
            Line = -7.5m,
            GameDate = new DateTime(2024, 9, 7, 19, 0, 0, DateTimeKind.Utc)
        };
        _context.Games.Add(existingGame);
        await _context.SaveChangesAsync();

        // Updated line
        var games = new List<GameSyncInput>
        {
            new("Alabama", "Auburn", -9.0m, new DateTime(2024, 9, 7, 19, 0, 0, DateTimeKind.Utc))
        };

        // Act
        var result = await _gameService.SyncGamesFromLinesAsync(2024, 1, games);

        // Assert
        result.Should().Be(1);

        var updatedGame = await _context.Games
            .FirstOrDefaultAsync(g => g.Year == 2024 && g.Week == 1 && g.Favorite == "Alabama");
        updatedGame.Should().NotBeNull();
        updatedGame!.Line.Should().Be(-9.0m);
    }

    [Fact]
    public async Task SyncGamesFromLinesAsync_WithMixedGames_CreatesAndUpdates()
    {
        // Arrange - Pre-create one game
        var existingGame = new GameEntity
        {
            Year = 2024,
            Week = 2,
            Favorite = "Ohio State",
            Underdog = "Michigan",
            Line = -3.5m,
            GameDate = new DateTime(2024, 9, 14, 12, 0, 0, DateTimeKind.Utc)
        };
        _context.Games.Add(existingGame);
        await _context.SaveChangesAsync();

        // One update, one new
        var games = new List<GameSyncInput>
        {
            new("Ohio State", "Michigan", -5.0m, new DateTime(2024, 9, 14, 12, 0, 0, DateTimeKind.Utc)), // Update
            new("Texas", "Oklahoma", -2.5m, new DateTime(2024, 9, 14, 15, 0, 0, DateTimeKind.Utc))  // New
        };

        // Act
        var result = await _gameService.SyncGamesFromLinesAsync(2024, 2, games);

        // Assert
        result.Should().Be(2);

        var gamesInDb = await _context.Games.Where(g => g.Year == 2024 && g.Week == 2).ToListAsync();
        gamesInDb.Should().HaveCount(2);

        var ohioState = gamesInDb.First(g => g.Favorite == "Ohio State");
        ohioState.Line.Should().Be(-5.0m);

        var texas = gamesInDb.First(g => g.Favorite == "Texas");
        texas.Line.Should().Be(-2.5m);
    }

    [Fact]
    public async Task SyncGamesFromLinesAsync_WithEmptyList_ReturnsZero()
    {
        // Arrange
        var games = new List<GameSyncInput>();

        // Act
        var result = await _gameService.SyncGamesFromLinesAsync(2024, 1, games);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task SyncGamesFromLinesAsync_UpdatesGameDate()
    {
        // Arrange - Pre-create a game with old date
        var oldDate = new DateTime(2024, 9, 7, 12, 0, 0, DateTimeKind.Utc);
        var newDate = new DateTime(2024, 9, 7, 19, 0, 0, DateTimeKind.Utc);

        var existingGame = new GameEntity
        {
            Year = 2024,
            Week = 1,
            Favorite = "Alabama",
            Underdog = "Auburn",
            Line = -7.5m,
            GameDate = oldDate
        };
        _context.Games.Add(existingGame);
        await _context.SaveChangesAsync();

        var games = new List<GameSyncInput>
        {
            new("Alabama", "Auburn", -7.5m, newDate)
        };

        // Act
        await _gameService.SyncGamesFromLinesAsync(2024, 1, games);

        // Assert
        var updatedGame = await _context.Games
            .FirstOrDefaultAsync(g => g.Year == 2024 && g.Week == 1 && g.Favorite == "Alabama");
        updatedGame!.GameDate.Should().Be(newDate);
    }

    #endregion

    #region GetWeekGamesAsync Tests

    [Fact]
    public async Task GetWeekGamesAsync_ReturnsAllGamesForWeek()
    {
        // Arrange
        var game1 = new GameEntity { Year = 2024, Week = 3, Favorite = "Team A", Underdog = "Team B", Line = -3m, GameDate = DateTime.UtcNow };
        var game2 = new GameEntity { Year = 2024, Week = 3, Favorite = "Team C", Underdog = "Team D", Line = -5m, GameDate = DateTime.UtcNow };
        var game3 = new GameEntity { Year = 2024, Week = 4, Favorite = "Team E", Underdog = "Team F", Line = -7m, GameDate = DateTime.UtcNow };

        _context.Games.AddRange(game1, game2, game3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _gameService.GetWeekGamesAsync(2024, 3);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(g => g.Favorite == "Team A");
        result.Should().Contain(g => g.Favorite == "Team C");
        result.Should().NotContain(g => g.Favorite == "Team E");
    }

    [Fact]
    public async Task GetWeekGamesAsync_WithNoGames_ReturnsEmptyList()
    {
        // Arrange - no games in database

        // Act
        var result = await _gameService.GetWeekGamesAsync(2024, 10);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetWeekGamesAsync_FiltersCorrectlyByYearAndWeek()
    {
        // Arrange
        var game1 = new GameEntity { Year = 2024, Week = 1, Favorite = "Team 2024-1", Underdog = "Other", Line = -3m, GameDate = DateTime.UtcNow };
        var game2 = new GameEntity { Year = 2023, Week = 1, Favorite = "Team 2023-1", Underdog = "Other", Line = -3m, GameDate = DateTime.UtcNow };
        var game3 = new GameEntity { Year = 2024, Week = 2, Favorite = "Team 2024-2", Underdog = "Other", Line = -3m, GameDate = DateTime.UtcNow };

        _context.Games.AddRange(game1, game2, game3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _gameService.GetWeekGamesAsync(2024, 1);

        // Assert
        result.Should().HaveCount(1);
        result.First().Favorite.Should().Be("Team 2024-1");
    }

    #endregion

    #region IsGameLockedAsync Tests

    [Fact]
    public async Task IsGameLockedAsync_WithPastKickoff_ReturnsTrue()
    {
        // Arrange
        var game = new GameEntity
        {
            Year = 2024,
            Week = 1,
            Favorite = "Past Game",
            Underdog = "Other",
            Line = -3m,
            GameDate = DateTime.UtcNow.AddHours(-1) // Past kickoff
        };
        _context.Games.Add(game);
        await _context.SaveChangesAsync();

        // Act
        var result = await _gameService.IsGameLockedAsync(game.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsGameLockedAsync_WithFutureKickoff_ReturnsFalse()
    {
        // Arrange
        var game = new GameEntity
        {
            Year = 2024,
            Week = 1,
            Favorite = "Future Game",
            Underdog = "Other",
            Line = -3m,
            GameDate = DateTime.UtcNow.AddHours(24) // Future kickoff
        };
        _context.Games.Add(game);
        await _context.SaveChangesAsync();

        // Act
        var result = await _gameService.IsGameLockedAsync(game.Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsGameLockedAsync_WithNonExistentGame_ReturnsNull()
    {
        // Arrange - no game in database

        // Act
        var result = await _gameService.IsGameLockedAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task IsGameLockedAsync_WithExactKickoffTime_ReturnsTrue()
    {
        // Arrange - Game exactly at current time should be locked
        var game = new GameEntity
        {
            Year = 2024,
            Week = 1,
            Favorite = "Exact Time Game",
            Underdog = "Other",
            Line = -3m,
            GameDate = DateTime.UtcNow // Exact kickoff
        };
        _context.Games.Add(game);
        await _context.SaveChangesAsync();

        // Act
        var result = await _gameService.IsGameLockedAsync(game.Id);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingGame_ReturnsGame()
    {
        // Arrange
        var game = new GameEntity
        {
            Year = 2024,
            Week = 1,
            Favorite = "Alabama",
            Underdog = "Auburn",
            Line = -7.5m,
            GameDate = DateTime.UtcNow
        };
        _context.Games.Add(game);
        await _context.SaveChangesAsync();

        // Act
        var result = await _gameService.GetByIdAsync(game.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Favorite.Should().Be("Alabama");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentGame_ReturnsNull()
    {
        // Arrange - no game

        // Act
        var result = await _gameService.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    #endregion
}
