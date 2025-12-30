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
/// Tests for BowlLeaderboardService implementation.
/// Uses InMemory database provider for testing.
/// </summary>
public class BowlLeaderboardServiceTests : IDisposable
{
    private readonly AtsDbContext _context;
    private readonly IBowlLeaderboardService _bowlLeaderboardService;
    private readonly Mock<ILogger<BowlLeaderboardService>> _mockLogger;
    private readonly Guid _user1Id;
    private readonly Guid _user2Id;

    public BowlLeaderboardServiceTests()
    {
        var options = new DbContextOptionsBuilder<AtsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AtsDbContext(options);
        _mockLogger = new Mock<ILogger<BowlLeaderboardService>>();
        _bowlLeaderboardService = new BowlLeaderboardService(_context, _mockLogger.Object);
        _user1Id = Guid.NewGuid();
        _user2Id = Guid.NewGuid();

        // Create test users
        _context.Users.AddRange(
            new User
            {
                Id = _user1Id,
                GoogleSubjectId = "google-user-1",
                Email = "user1@test.com",
                DisplayName = "User One",
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow,
                IsActive = true
            },
            new User
            {
                Id = _user2Id,
                GoogleSubjectId = "google-user-2",
                Email = "user2@test.com",
                DisplayName = "User Two",
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow,
                IsActive = true
            }
        );
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region Helper Methods

    private BowlGameEntity CreateBowlGame(int year, int gameNumber, string favorite, string underdog, decimal line,
        string? spreadWinner = null, string? outrightWinner = null, int? favoriteScore = null, int? underdogScore = null)
    {
        var game = new BowlGameEntity
        {
            Year = year,
            GameNumber = gameNumber,
            BowlName = $"Bowl {gameNumber}",
            Favorite = favorite,
            Underdog = underdog,
            Line = line,
            GameDate = DateTime.UtcNow.AddDays(-1),
            SpreadWinner = spreadWinner,
            OutrightWinner = outrightWinner,
            FavoriteScore = favoriteScore,
            UnderdogScore = underdogScore,
            IsPush = spreadWinner == null && favoriteScore != null
        };
        _context.BowlGames.Add(game);
        _context.SaveChanges();
        return game;
    }

    private void CreateBowlPick(Guid userId, int bowlGameId, string spreadPick, int confidencePoints, string outrightPick, int year)
    {
        _context.BowlPicks.Add(new BowlPickEntity
        {
            UserId = userId,
            BowlGameId = bowlGameId,
            SpreadPick = spreadPick,
            ConfidencePoints = confidencePoints,
            OutrightWinnerPick = outrightPick,
            SubmittedAt = DateTime.UtcNow,
            Year = year
        });
        _context.SaveChanges();
    }

    #endregion

    #region GetBowlLeaderboardAsync Tests

    [Fact]
    public async Task GetBowlLeaderboardAsync_CalculatesPointsCorrectly()
    {
        // Arrange
        var game1 = CreateBowlGame(2024, 1, "USC", "Penn State", -7m, "USC", "USC", 35, 21); // User 1 wins 10 pts
        var game2 = CreateBowlGame(2024, 2, "Georgia", "Notre Dame", -3m, "Notre Dame", "Georgia", 21, 20); // User 1 loses

        CreateBowlPick(_user1Id, game1.Id, "USC", 10, "USC", 2024);     // Correct spread, 10 pts
        CreateBowlPick(_user1Id, game2.Id, "Georgia", 5, "Georgia", 2024); // Wrong spread, 0 pts

        // Act
        var result = await _bowlLeaderboardService.GetBowlLeaderboardAsync(2024);

        // Assert
        result.Should().HaveCount(1);
        var entry = result.First();
        entry.SpreadPoints.Should().Be(10);
        entry.SpreadWins.Should().Be(1);
        entry.SpreadLosses.Should().Be(1);
        entry.MaxPossiblePoints.Should().Be(15);
    }

    [Fact]
    public async Task GetBowlLeaderboardAsync_HandlesOutrightWinsCorrectly()
    {
        // Arrange
        var game1 = CreateBowlGame(2024, 1, "USC", "Penn State", -7m, "USC", "USC", 35, 21);
        var game2 = CreateBowlGame(2024, 2, "Georgia", "Notre Dame", -3m, "Notre Dame", "Georgia", 21, 20);

        CreateBowlPick(_user1Id, game1.Id, "USC", 10, "USC", 2024);        // Correct outright
        CreateBowlPick(_user1Id, game2.Id, "Georgia", 5, "Notre Dame", 2024); // Wrong outright

        // Act
        var result = await _bowlLeaderboardService.GetBowlLeaderboardAsync(2024);

        // Assert
        result.Should().HaveCount(1);
        result.First().OutrightWins.Should().Be(1);
    }

    [Fact]
    public async Task GetBowlLeaderboardAsync_HandlesPushCorrectly()
    {
        // Arrange
        var game = CreateBowlGame(2024, 1, "USC", "Penn State", -7m, null, "USC", 28, 21); // Push (exactly 7)
        game.IsPush = true;
        await _context.SaveChangesAsync();

        CreateBowlPick(_user1Id, game.Id, "USC", 10, "USC", 2024);

        // Act
        var result = await _bowlLeaderboardService.GetBowlLeaderboardAsync(2024);

        // Assert
        result.Should().HaveCount(1);
        var entry = result.First();
        entry.SpreadPoints.Should().Be(0); // Push = no points
        entry.SpreadPushes.Should().Be(1);
        entry.SpreadWins.Should().Be(0);
        entry.SpreadLosses.Should().Be(0);
    }

    [Fact]
    public async Task GetBowlLeaderboardAsync_SortsLeaderboardCorrectly()
    {
        // Arrange - User2 scores more than User1
        var game1 = CreateBowlGame(2024, 1, "USC", "Penn State", -7m, "USC", "USC", 35, 21);
        var game2 = CreateBowlGame(2024, 2, "Georgia", "Notre Dame", -3m, "Georgia", "Georgia", 28, 14);

        CreateBowlPick(_user1Id, game1.Id, "USC", 5, "USC", 2024);      // 5 pts
        CreateBowlPick(_user1Id, game2.Id, "Notre Dame", 10, "Georgia", 2024); // 0 pts

        CreateBowlPick(_user2Id, game1.Id, "USC", 10, "USC", 2024);     // 10 pts
        CreateBowlPick(_user2Id, game2.Id, "Georgia", 5, "Georgia", 2024); // 5 pts

        // Act
        var result = await _bowlLeaderboardService.GetBowlLeaderboardAsync(2024);

        // Assert
        result.Should().HaveCount(2);
        result[0].UserId.Should().Be(_user2Id); // 15 pts total
        result[0].SpreadPoints.Should().Be(15);
        result[1].UserId.Should().Be(_user1Id); // 5 pts total
        result[1].SpreadPoints.Should().Be(5);
    }

    [Fact]
    public async Task GetBowlLeaderboardAsync_CalculatesGamesCompletedCorrectly()
    {
        // Arrange
        var completedGame = CreateBowlGame(2024, 1, "USC", "Penn State", -7m, "USC", "USC", 35, 21);
        var pendingGame = CreateBowlGame(2024, 2, "Georgia", "Notre Dame", -3m); // No result

        CreateBowlPick(_user1Id, completedGame.Id, "USC", 10, "USC", 2024);
        CreateBowlPick(_user1Id, pendingGame.Id, "Georgia", 5, "Georgia", 2024);

        // Act
        var result = await _bowlLeaderboardService.GetBowlLeaderboardAsync(2024);

        // Assert
        result.Should().HaveCount(1);
        result.First().GamesCompleted.Should().Be(1);
        result.First().TotalGames.Should().Be(2);
    }

    [Fact]
    public async Task GetBowlLeaderboardAsync_WithNoPicksForYear_ReturnsEmptyList()
    {
        // Arrange - no picks

        // Act
        var result = await _bowlLeaderboardService.GetBowlLeaderboardAsync(2024);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetUserBowlHistoryAsync Tests

    [Fact]
    public async Task GetUserBowlHistoryAsync_ReturnsUserHistory()
    {
        // Arrange
        var game1 = CreateBowlGame(2024, 1, "USC", "Penn State", -7m, "USC", "USC", 35, 21);
        var game2 = CreateBowlGame(2024, 2, "Georgia", "Notre Dame", -3m, "Notre Dame", "Georgia", 21, 20);

        CreateBowlPick(_user1Id, game1.Id, "USC", 10, "USC", 2024);
        CreateBowlPick(_user1Id, game2.Id, "Georgia", 5, "Georgia", 2024);

        // Act
        var result = await _bowlLeaderboardService.GetUserBowlHistoryAsync(_user1Id, 2024);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(_user1Id);
        result.DisplayName.Should().Be("User One");
        result.Year.Should().Be(2024);
        result.TotalPoints.Should().Be(10); // Only game 1 correct
        result.MaxPossiblePoints.Should().Be(15);
        result.Picks.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUserBowlHistoryAsync_CalculatesPickDetailsCorrectly()
    {
        // Arrange
        var game = CreateBowlGame(2024, 1, "USC", "Penn State", -7m, "USC", "USC", 35, 21);
        CreateBowlPick(_user1Id, game.Id, "USC", 10, "Penn State", 2024);

        // Act
        var result = await _bowlLeaderboardService.GetUserBowlHistoryAsync(_user1Id, 2024);

        // Assert
        result.Should().NotBeNull();
        var pick = result!.Picks.First();
        pick.GameNumber.Should().Be(1);
        pick.BowlName.Should().Be("Bowl 1");
        pick.SpreadPick.Should().Be("USC");
        pick.ConfidencePoints.Should().Be(10);
        pick.OutrightWinnerPick.Should().Be("Penn State");
        pick.HasResult.Should().BeTrue();
        pick.FavoriteScore.Should().Be(35);
        pick.UnderdogScore.Should().Be(21);
        pick.SpreadPickCorrect.Should().BeTrue();
        pick.OutrightPickCorrect.Should().BeFalse();
        pick.PointsEarned.Should().Be(10);
    }

    [Fact]
    public async Task GetUserBowlHistoryAsync_HandlesPushInDetails()
    {
        // Arrange
        var game = CreateBowlGame(2024, 1, "USC", "Penn State", -7m, null, "USC", 28, 21);
        game.IsPush = true;
        await _context.SaveChangesAsync();

        CreateBowlPick(_user1Id, game.Id, "USC", 10, "USC", 2024);

        // Act
        var result = await _bowlLeaderboardService.GetUserBowlHistoryAsync(_user1Id, 2024);

        // Assert
        result.Should().NotBeNull();
        var pick = result!.Picks.First();
        pick.IsPush.Should().BeTrue();
        pick.SpreadPickCorrect.Should().BeNull(); // Push is neither correct nor incorrect
        pick.PointsEarned.Should().Be(0);
    }

    [Fact]
    public async Task GetUserBowlHistoryAsync_WithNonExistentUser_ReturnsNull()
    {
        // Arrange - no user

        // Act
        var result = await _bowlLeaderboardService.GetUserBowlHistoryAsync(Guid.NewGuid(), 2024);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserBowlHistoryAsync_WithNoPicksForYear_ReturnsNull()
    {
        // Arrange - user exists but no picks

        // Act
        var result = await _bowlLeaderboardService.GetUserBowlHistoryAsync(_user1Id, 2024);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserBowlHistoryAsync_PicksAreOrderedByGameNumber()
    {
        // Arrange
        var game3 = CreateBowlGame(2024, 3, "Team A", "Team B", -3m);
        var game1 = CreateBowlGame(2024, 1, "Team C", "Team D", -5m);
        var game2 = CreateBowlGame(2024, 2, "Team E", "Team F", -7m);

        CreateBowlPick(_user1Id, game3.Id, "Team A", 3, "Team A", 2024);
        CreateBowlPick(_user1Id, game1.Id, "Team C", 1, "Team C", 2024);
        CreateBowlPick(_user1Id, game2.Id, "Team E", 2, "Team E", 2024);

        // Act
        var result = await _bowlLeaderboardService.GetUserBowlHistoryAsync(_user1Id, 2024);

        // Assert
        result.Should().NotBeNull();
        result!.Picks.Should().HaveCount(3);
        result.Picks[0].GameNumber.Should().Be(1);
        result.Picks[1].GameNumber.Should().Be(2);
        result.Picks[2].GameNumber.Should().Be(3);
    }

    #endregion
}
